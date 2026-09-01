using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Certifications.Application.Contracts;
using Certifications.Domain.Enums;

namespace Certifications.Tests.Integration;

[Collection(ApiCollection.Name)]
public sealed class RestApiTests(ApiFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task Swagger_IsAvailableInDevelopment_WithExpectedSecurityMetadata()
    {
        using var client = fixture.CreateClient(includeApiKey: false);

        using (var uiResponse = await client.GetAsync("/swagger/index.html"))
        {
            Assert.Equal(HttpStatusCode.OK, uiResponse.StatusCode);
            Assert.Contains(
                "Swagger UI",
                await uiResponse.Content.ReadAsStringAsync(),
                StringComparison.OrdinalIgnoreCase);
        }

        using var documentResponse = await client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, documentResponse.StatusCode);

        await using var documentStream = await documentResponse.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(documentStream);
        var root = document.RootElement;

        Assert.StartsWith("3.0", root.GetProperty("openapi").GetString());

        var schemes = root
            .GetProperty("components")
            .GetProperty("securitySchemes");
        var schemas = root
            .GetProperty("components")
            .GetProperty("schemas");
        Assert.True(schemes.TryGetProperty("ApiKey", out _));
        Assert.True(schemes.TryGetProperty("CookieAuth", out _));

        var globalSecurity = root.GetProperty("security")[0];
        Assert.True(globalSecurity.TryGetProperty("ApiKey", out _));
        Assert.False(globalSecurity.TryGetProperty("CookieAuth", out _));

        AssertStringEnumSchema(
            root,
            nameof(AdminMode),
            nameof(AdminMode.MyPage),
            nameof(AdminMode.Administration));
        AssertStringEnumSchema(
            root,
            nameof(CertificationStatus),
            nameof(CertificationStatus.NotApplicable),
            nameof(CertificationStatus.ContractValid),
            nameof(CertificationStatus.CertificationPending),
            nameof(CertificationStatus.CertificationInProgress),
            nameof(CertificationStatus.CertificationMissing));

        AssertRequiredAndNullableMetadata(schemas);
        AssertRequiredProperties(
            schemas,
            nameof(CurrentUserDto),
            "employeeId",
            "personalId",
            "firstName",
            "lastName",
            "displayName",
            "isAdmin");
        AssertRequiredProperties(
            schemas,
            nameof(CreateContractRequest),
            "position",
            "contractDate");
        AssertRequiredProperties(
            schemas,
            nameof(ContractDetailsDto),
            "contract",
            "certifications");
        AssertNullableReferenceProperty(
            schemas,
            nameof(CurrentUserDto),
            "preferredAdminMode",
            nameof(AdminMode));
        AssertNullableReferenceProperty(
            schemas,
            nameof(EmployeeDetailsDto),
            "currentContract",
            nameof(ContractDetailsDto));

        var paths = root.GetProperty("paths");
        var login = paths
            .GetProperty("/api/v1/auth/login")
            .GetProperty("post");
        Assert.Equal("Login", login.GetProperty("operationId").GetString());
        Assert.False(login.TryGetProperty("security", out _));

        var logoutSecurity = paths
            .GetProperty("/api/v1/auth/logout")
            .GetProperty("post")
            .GetProperty("security")[0];
        Assert.True(logoutSecurity.TryGetProperty("ApiKey", out _));
        Assert.True(logoutSecurity.TryGetProperty("CookieAuth", out _));
    }

    [Fact]
    public async Task ApiKey_IsRequiredBeforeAuthentication()
    {
        using var client = fixture.CreateClient(includeApiKey: false);

        using var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest("КП-0001", ApiFixture.BootstrapPassword),
            JsonOptions);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ForwardedHttps_AllowsSecureAntiforgeryCookieBehindReverseProxy()
    {
        using var client = fixture.CreateClient(useHttps: false, handleCookies: false);
        using var loginRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login")
        {
            Content = JsonContent.Create(
                new LoginRequest("КП-0001", ApiFixture.BootstrapPassword),
                options: JsonOptions)
        };
        AddForwardedHttpsHeaders(loginRequest);

        using var loginResponse = await client.SendAsync(loginRequest);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var authenticationCookie = Assert.Single(
            loginResponse.Headers.GetValues("Set-Cookie"));
        Assert.Contains("secure", authenticationCookie, StringComparison.OrdinalIgnoreCase);

        using var csrfRequest = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/v1/auth/csrf-token");
        AddForwardedHttpsHeaders(csrfRequest);
        csrfRequest.Headers.TryAddWithoutValidation(
            "Cookie",
            authenticationCookie.Split(';', 2)[0]);

        using var csrfResponse = await client.SendAsync(csrfRequest);
        Assert.Equal(HttpStatusCode.OK, csrfResponse.StatusCode);
        Assert.NotNull(await csrfResponse.Content.ReadFromJsonAsync<CsrfTokenDto>(JsonOptions));
        var antiforgeryCookie = csrfResponse.Headers
            .GetValues("Set-Cookie")
            .Single(value => value.StartsWith(
                "Certifications.Antiforgery=",
                StringComparison.Ordinal));
        Assert.Contains("secure", antiforgeryCookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CurrentUserResponses_IncludeStructuredNames()
    {
        using var client = fixture.CreateClient();

        using var loginResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest("КП-0001", ApiFixture.BootstrapPassword),
            JsonOptions);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        AssertNoStore(loginResponse);
        var loginUser = await ReadAsync<CurrentUserDto>(loginResponse);

        Assert.Equal("Елена", loginUser.FirstName);
        Assert.Equal("Сергеевна", loginUser.MiddleName);
        Assert.Equal("Волкова", loginUser.LastName);
        Assert.Equal("Елена Сергеевна Волкова", loginUser.DisplayName);

        using var currentUserResponse = await client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.OK, currentUserResponse.StatusCode);
        var currentUser = await ReadAsync<CurrentUserDto>(currentUserResponse);

        Assert.Equal(loginUser, currentUser);
    }

    [Fact]
    public async Task EmployeeAndCertificationWorkflow_EnforcesSecurityAndBusinessRules()
    {
        using var admin = fixture.CreateClient();
        await LoginAsync(admin, " кп-0001 ", ApiFixture.BootstrapPassword);

        using (var noCsrf = await admin.PostAsJsonAsync(
                   "/api/v1/employees",
                   CreateEmployee("CSRF-TEST"),
                   JsonOptions))
        {
            Assert.Equal(HttpStatusCode.BadRequest, noCsrf.StatusCode);
        }

        var adminCsrf = await GetCsrfAsync(admin);
        using (var preferredMode = await SendCommandAsync(
                   admin,
                   HttpMethod.Put,
                   "/api/v1/auth/preferred-mode",
                   new { PreferredMode = nameof(AdminMode.Administration) },
                   adminCsrf))
        {
            Assert.Equal(HttpStatusCode.NoContent, preferredMode.StatusCode);
        }

        var createRequest = CreateEmployee("  кп-9001 ");
        using var createResponse = await SendCommandAsync(
            admin,
            HttpMethod.Post,
            "/api/v1/employees",
            createRequest,
            adminCsrf);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        AssertNoStore(createResponse);
        var created = await ReadAsync<CreateEmployeeResultDto>(createResponse);
        Assert.Matches("^(?=.*[A-Za-z])(?=.*[0-9])[A-Za-z0-9]{16}$", created.GeneratedPassword);
        Assert.NotNull(created.Employee.CurrentContract);
        var employeeId = created.Employee.EmployeeId;
        var contract = created.Employee.CurrentContract!.Contract;

        using (var employees = await admin.GetAsync("/api/v1/employees?name=9001"))
        {
            Assert.Equal(HttpStatusCode.OK, employees.StatusCode);
            Assert.Single((await ReadAsync<PagedResult<EmployeeSummaryDto>>(employees)).Items);
        }

        using (var duplicate = await SendCommandAsync(
                   admin,
                   HttpMethod.Post,
                   "/api/v1/employees",
                   CreateEmployee("К П - 9 0 0 1"),
                   adminCsrf))
        {
            Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        }

        using (var secondContract = await SendCommandAsync(
                   admin,
                   HttpMethod.Post,
                   $"/api/v1/employees/{employeeId}/contracts",
                   createRequest.FirstContract,
                   adminCsrf))
        {
            Assert.Equal(HttpStatusCode.Conflict, secondContract.StatusCode);
        }

        using var createCertification = await SendCommandAsync(
            admin,
            HttpMethod.Post,
            $"/api/v1/contracts/{contract.ContractId}/certifications",
            new CreateCertificationRequest(
                "Полковник Морозов",
                new DateOnly(2026, 8, 10)),
            adminCsrf);
        Assert.Equal(HttpStatusCode.Created, createCertification.StatusCode);
        var certification = await ReadAsync<CertificationDto>(createCertification);

        using (var secondCertification = await SendCommandAsync(
                   admin,
                   HttpMethod.Post,
                   $"/api/v1/contracts/{contract.ContractId}/certifications",
                   new CreateCertificationRequest(
                       "Майор Волков",
                       new DateOnly(2026, 8, 20)),
                   adminCsrf))
        {
            Assert.Equal(HttpStatusCode.Conflict, secondCertification.StatusCode);
        }

        using var updateCertification = await SendCommandAsync(
            admin,
            HttpMethod.Patch,
            $"/api/v1/certifications/{certification.CertificationId}",
            new UpdateCertificationRequest(
                certification.Assessor,
                certification.CertificationDate,
                new DateOnly(2026, 8, 11),
                new DateOnly(2026, 8, 12)),
            adminCsrf);
        Assert.Equal(HttpStatusCode.OK, updateCertification.StatusCode);

        using var returnResponse = await SendCommandAsync(
            admin,
            HttpMethod.Post,
            $"/api/v1/certifications/{certification.CertificationId}/return",
            new ReturnCertificationRequest(new DateOnly(2026, 8, 13), contract.RowVersion),
            adminCsrf);
        Assert.Equal(HttpStatusCode.OK, returnResponse.StatusCode);
        var returned = await ReadAsync<ReturnCertificationResultDto>(returnResponse);
        Assert.True(returned.Certification.IsCompleted);
        Assert.Equal(new DateOnly(2027, 8, 11), returned.Contract.ValidTo);
        Assert.Equal(CertificationStatus.ContractValid, returned.Contract.Status);

        using (var editCompleted = await SendCommandAsync(
                   admin,
                   HttpMethod.Patch,
                   $"/api/v1/certifications/{certification.CertificationId}",
                   new UpdateCertificationRequest(
                       certification.Assessor,
                       certification.CertificationDate,
                       new DateOnly(2026, 8, 11),
                       new DateOnly(2026, 8, 12)),
                   adminCsrf))
        {
            Assert.Equal(HttpStatusCode.Conflict, editCompleted.StatusCode);
        }

        using var overviewResponse = await admin.GetAsync(
            "/api/v1/certifications/overview?name=9001&sort=effectiveValidTo&direction=asc");
        var overviewJson = await overviewResponse.Content.ReadAsStringAsync();
        Assert.True(overviewResponse.IsSuccessStatusCode, overviewJson);
        using (var overviewDocument = JsonDocument.Parse(overviewJson))
        {
            var status = overviewDocument.RootElement
                .GetProperty("items")[0]
                .GetProperty("status");
            Assert.Equal(JsonValueKind.String, status.ValueKind);
            Assert.Equal(nameof(CertificationStatus.ContractValid), status.GetString());
        }

        var overview = JsonSerializer.Deserialize<PagedResult<CertificationOverviewRowDto>>(
            overviewJson,
            JsonOptions);
        var overviewRow = Assert.Single(
            Assert.IsType<PagedResult<CertificationOverviewRowDto>>(overview).Items);
        Assert.Equal(employeeId, overviewRow.EmployeeId);
        Assert.True(overviewRow.LatestCertification?.IsCompleted);

        using (var filteredOverview = await admin.GetAsync(
                   "/api/v1/certifications/overview"
                   + "?department=%D0%9E%D1%82%D0%B4%D0%B5%D0%BB"
                   + "&status=ContractValid"
                   + "&validToFrom=2027-01-01"
                   + "&validToTo=2027-12-31"
                   + "&sort=status&direction=desc"))
        {
            Assert.Equal(HttpStatusCode.OK, filteredOverview.StatusCode);
            Assert.Contains(
                (await ReadAsync<PagedResult<CertificationOverviewRowDto>>(filteredOverview)).Items,
                row => row.EmployeeId == employeeId);
        }

        using var employee = fixture.CreateClient();
        await LoginAsync(employee, "КП-9001", created.GeneratedPassword);
        using (var forbidden = await employee.GetAsync("/api/v1/employees"))
        {
            Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        }

        using (var ownContract = await employee.GetAsync("/api/v1/me/contract"))
        {
            Assert.Equal(HttpStatusCode.OK, ownContract.StatusCode);
        }

        var employeeCsrf = await GetCsrfAsync(employee);
        using (var password = await SendCommandAsync<object?>(
                   employee,
                   HttpMethod.Post,
                   "/api/v1/me/password/reveal",
                   null,
                   employeeCsrf))
        {
            Assert.Equal(HttpStatusCode.OK, password.StatusCode);
            AssertNoStore(password);
            Assert.Equal(created.GeneratedPassword, (await ReadAsync<PasswordDto>(password)).Password);
        }

        using (var staleClose = await SendCommandAsync(
                   admin,
                   HttpMethod.Post,
                   $"/api/v1/contracts/{contract.ContractId}/close",
                   new CloseContractRequest(new DateOnly(2026, 8, 27), contract.RowVersion),
                   adminCsrf))
        {
            Assert.Equal(HttpStatusCode.Conflict, staleClose.StatusCode);
        }

        using (var close = await SendCommandAsync(
                   admin,
                   HttpMethod.Post,
                   $"/api/v1/contracts/{contract.ContractId}/close",
                   new CloseContractRequest(new DateOnly(2026, 8, 27), returned.Contract.RowVersion),
                   adminCsrf))
        {
            Assert.Equal(HttpStatusCode.NoContent, close.StatusCode);
        }

        using var accessAfterClose = await employee.GetAsync("/api/v1/me/contract");
        Assert.Equal(HttpStatusCode.Forbidden, accessAfterClose.StatusCode);
    }

    private static CreateEmployeeRequest CreateEmployee(string personalId) =>
        new(
            personalId,
            "Илья",
            "Сергеевич",
            "Кузнецов",
            false,
            new CreateContractRequest(
                "Оперуполномоченный",
                "Отдел криминальной полиции",
                "Криминальная полиция",
                new DateOnly(2026, 1, 1),
                null,
                null,
                null,
                null));

    private static async Task LoginAsync(HttpClient client, string personalId, string password)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(personalId, password),
            JsonOptions);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AssertNoStore(response);
    }

    private static void AddForwardedHttpsHeaders(HttpRequestMessage request)
    {
        request.Headers.Add("X-Forwarded-For", "127.0.0.1");
        request.Headers.Add("X-Forwarded-Proto", "https");
    }

    private static async Task<string> GetCsrfAsync(HttpClient client)
    {
        var token = await client.GetFromJsonAsync<CsrfTokenDto>(
            "/api/v1/auth/csrf-token",
            JsonOptions);
        return Assert.IsType<CsrfTokenDto>(token).RequestToken;
    }

    private static async Task<HttpResponseMessage> SendCommandAsync<T>(
        HttpClient client,
        HttpMethod method,
        string uri,
        T body,
        string csrfToken)
    {
        var request = new HttpRequestMessage(method, uri)
        {
            Content = body is null ? null : JsonContent.Create(body, options: JsonOptions)
        };
        request.Headers.Add("X-CSRF-TOKEN", csrfToken);
        return await client.SendAsync(request);
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        var value = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
        return Assert.IsType<T>(value);
    }

    private static void AssertNoStore(HttpResponseMessage response) =>
        Assert.Contains("no-store", response.Headers.CacheControl?.ToString() ?? string.Empty);

    private static void AssertStringEnumSchema(
        JsonElement root,
        string schemaName,
        params string[] expectedValues)
    {
        var schema = root
            .GetProperty("components")
            .GetProperty("schemas")
            .GetProperty(schemaName);

        Assert.Equal("string", schema.GetProperty("type").GetString());
        Assert.False(schema.TryGetProperty("format", out _));
        Assert.Equal(
            expectedValues,
            schema.GetProperty("enum")
                .EnumerateArray()
                .Select(value => value.GetString())
                .ToArray());
    }

    private static void AssertRequiredAndNullableMetadata(JsonElement schemas)
    {
        foreach (var schemaProperty in schemas.EnumerateObject())
        {
            if (!schemaProperty.Value.TryGetProperty("properties", out var properties)
                || schemaProperty.Name == "ProblemDetails")
            {
                continue;
            }

            Assert.True(
                schemaProperty.Value.TryGetProperty("required", out var required),
                $"Schema '{schemaProperty.Name}' has no required metadata.");
            var requiredNames = required
                .EnumerateArray()
                .Select(value => Assert.IsType<string>(value.GetString()))
                .ToHashSet(StringComparer.Ordinal);
            Assert.NotEmpty(requiredNames);

            foreach (var property in properties.EnumerateObject())
            {
                var isNullable = property.Value.TryGetProperty("nullable", out var nullable)
                    && nullable.GetBoolean();
                Assert.Equal(!isNullable, requiredNames.Contains(property.Name));
            }
        }
    }

    private static void AssertRequiredProperties(
        JsonElement schemas,
        string schemaName,
        params string[] expectedProperties)
    {
        var actualProperties = schemas
            .GetProperty(schemaName)
            .GetProperty("required")
            .EnumerateArray()
            .Select(value => Assert.IsType<string>(value.GetString()))
            .ToHashSet(StringComparer.Ordinal);

        Assert.True(
            actualProperties.SetEquals(expectedProperties),
            $"Schema '{schemaName}' required [{string.Join(", ", actualProperties)}].");
    }

    private static void AssertNullableReferenceProperty(
        JsonElement schemas,
        string schemaName,
        string propertyName,
        string referencedSchemaName)
    {
        var property = schemas
            .GetProperty(schemaName)
            .GetProperty("properties")
            .GetProperty(propertyName);

        Assert.True(property.GetProperty("nullable").GetBoolean());
        Assert.Equal(
            $"#/components/schemas/{referencedSchemaName}",
            property.GetProperty("allOf")[0].GetProperty("$ref").GetString());
    }
}
