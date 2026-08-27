# Backend instructions

These instructions extend the repository-level `AGENTS.md` for work under `backend/`.

## Technology

- Target .NET 10.
- Use ASP.NET Core Minimal API.
- Use PostgreSQL through Entity Framework Core 10 and the PostgreSQL EF provider.
- Organize code into API, Application, Domain, Infrastructure, and Tests projects as described in `../ApiDesign.md`.

## Architecture

- Keep route handlers thin: bind input, invoke an application use case, and map the result.
- Put business rules in Domain/Application, not in `Program.cs`, endpoint mappings, EF configurations, or Angular.
- Group endpoints by feature with `MapGroup` extension methods.
- Use request/response DTOs; never serialize EF entities directly.
- Use `DateOnly` for business dates.
- Calculate `EffectiveValidTo` and `CertificationStatus` on the backend.
- Complete certification and update `Contract.ValidTo` in one database transaction.
- Enforce one active contract per employee in application logic and with a PostgreSQL partial unique index.
- Use an immutable technical `Employee.Id`; treat `PersonalId` as a mutable unique login.

## Authentication and secrets

- Validate `X-API-Key` before protected API processing.
- Establish user authentication with a secure `HttpOnly` cookie after login.
- Apply CSRF protection to state-changing cookie-authenticated requests.
- Re-check current administrative rights and active-contract eligibility from server-side data.
- Bind `Security:ApiKey` and `Security:PasswordEncryptionKey` through typed options with startup validation.
- Never include encrypted passwords in ordinary DTOs.
- Password generate/reveal endpoints must use `Cache-Control: no-store` and must not log plaintext.

## Persistence

- Keep EF Core configuration and migrations in Infrastructure.
- Use explicit foreign keys and indexes.
- Use optimistic concurrency for mutable contracts.
- Normalize `PersonalId` consistently for create, update, and login.
- Avoid N+1 queries in certification overview endpoints.
- Apply filtering, sorting, and pagination in PostgreSQL, not in memory.

## API conventions

- Base path: `/api/v1`.
- Return `ProblemDetails` or `ValidationProblemDetails` for errors.
- Use stable OpenAPI operation names via `WithName`.
- Use `409 Conflict` for uniqueness and business-state conflicts.
- Use explicit command endpoints for irreversible transitions such as certification return and contract close.

## Verification

After the solution exists, run the narrowest applicable commands:

```bash
dotnet format --verify-no-changes
dotnet build
dotnet test
```

For database changes, also verify that the migration can be generated and applied to a disposable PostgreSQL database. Do not apply migrations to a user database without explicit authorization.

