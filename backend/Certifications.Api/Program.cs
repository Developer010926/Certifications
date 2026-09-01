using System.Reflection;
using System.Text.Json.Serialization;
using Certifications.Api.Configuration;
using Certifications.Api.Endpoints;
using Certifications.Api.Errors;
using Certifications.Api.OpenApi;
using Certifications.Api.Security;
using Certifications.Application;
using Certifications.Infrastructure;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);
var isOpenApiGeneration = Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider";

if (isOpenApiGeneration)
{
    builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["ConnectionStrings:DefaultConnection"] =
            "Host=localhost;Database=openapi;Username=openapi;Password=openapi",
        ["Security:ApiKey"] = new string('a', 32),
        ["Security:PasswordEncryptionKey"] =
            Convert.ToBase64String(new byte[32])
    });
}

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "ConnectionStrings:DefaultConnection must be configured.");
}

var cookieSettings = builder.Configuration
    .GetSection(CookieSettings.SectionName)
    .Get<CookieSettings>() ?? new CookieSettings();
var corsSettings = builder.Configuration
    .GetSection(CorsSettings.SectionName)
    .Get<CorsSettings>() ?? new CorsSettings();

if (string.IsNullOrWhiteSpace(cookieSettings.CookieName)
    || cookieSettings.ExpireMinutes is < 1 or > 1440)
{
    throw new InvalidOperationException("Authentication configuration is invalid.");
}

builder.Services.AddApplication();
builder.Services.AddInfrastructure(
    connectionString,
    builder.Configuration,
    addBootstrapHostedService: !isOpenApiGeneration);
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = cookieSettings.CookieName;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.IsEssential = true;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(cookieSettings.ExpireMinutes);
        options.SlidingExpiration = false;
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        "ActiveContractRequired",
        policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.AddRequirements(new ActiveContractRequirement());
        });
    options.AddPolicy(
        "AdminOnly",
        policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.AddRequirements(new AdminRequirement());
        });
});
builder.Services.AddScoped<IAuthorizationHandler, ActiveContractAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, AdminAuthorizationHandler>();
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "Certifications.Antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        if (corsSettings.AllowedOrigins.Length > 0)
        {
            policy.WithOrigins(corsSettings.AllowedOrigins)
                .AllowCredentials()
                .WithHeaders("Content-Type", "X-API-Key", "X-CSRF-TOKEN")
                .WithMethods("GET", "POST", "PUT", "PATCH");
        }
    });
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SupportNonNullableReferenceTypes();
    options.NonNullableReferenceTypesAsRequired();
    options.UseAllOfToExtendReferenceSchemas();
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Certifications.Api | v1",
        Version = "1.0.0"
    });
    options.DocumentFilter<ApiSecurityDocumentFilter>();
    options.SchemaFilter<StringEnumSchemaFilter>();
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
        options.SwaggerEndpoint("v1/swagger.json", "Certifications API v1"));
}

app.UseExceptionHandler();
app.UseCors("Frontend");
app.UseMiddleware<ApiKeyMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapApiEndpoints();
app.Run();

public partial class Program;
