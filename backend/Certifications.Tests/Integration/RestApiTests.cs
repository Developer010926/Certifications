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
        Assert.True(schemes.TryGetProperty("ApiKey", out _));
        Assert.True(schemes.TryGetProperty("CookieAuth", out _));

        var globalSecurity = root.GetProperty("security")[0];
        Assert.True(globalSecurity.TryGetProperty("ApiKey", out _));
        Assert.False(globalSecurity.TryGetProperty("CookieAuth", out _));

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
}
