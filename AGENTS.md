# Repository instructions

## Scope and sources of truth

- This repository contains one product with an ASP.NET Core API and an Angular UI.
- Read `Requirements.md` before changing behavior.
- Read `ApiDesign.md` for backend/API decisions and `UiDesign.md` for frontend decisions.
- Treat `Certifications.xmi` and `certifications.mdj` as design inputs. Do not edit them unless the user explicitly asks for model changes.
- If implementation and documentation disagree, stop and report the conflict instead of silently choosing one.

## Repository boundaries

- Backend code belongs under `backend/`.
- Frontend code belongs under `frontend/`.
- Shared product documentation remains in the repository root.
- Keep backend and frontend independently buildable, but use a single Git repository and a single OpenAPI contract.
- Do not move files between backend and frontend without an explicit architectural reason.

## Cross-layer contract

- The backend owns business rules, authorization, status calculation, and validation.
- The frontend consumes DTOs and must not reproduce domain calculations as an independent source of truth.
- Never expose persistence entities directly through REST endpoints.
- When an API contract changes, update OpenAPI metadata and regenerate the Angular client.
- Do not manually edit generated API client files.

## Security

- Never commit real API keys, password-encryption keys, passwords, connection strings, or production cookies.
- Keep placeholders in committed `appsettings.json`; load real values from an ignored environment-specific appsettings file or environment variables.
- The API key is an additional request barrier, not user authentication.
- User authentication uses a secure `HttpOnly` cookie after login.
- Do not place secrets in Angular source, browser storage, URLs, logs, test snapshots, or generated files.
- Password reveal responses must not be cached or logged.

## Working rules

- Preserve unrelated user changes.
- Prefer small, reviewable changes.
- Add or update tests when behavior changes.
- Use the narrowest relevant build and test commands first.
- Do not add production dependencies without explaining why they are needed.
- Do not generate database migrations unless the requested model change is finalized.
- Do not perform destructive Git or database operations without explicit approval.

## Verification

- Backend changes: follow `backend/AGENTS.md`.
- Frontend changes: follow `frontend/AGENTS.md`.
- Cross-layer changes: verify both projects and confirm the OpenAPI/Angular client contract remains synchronized.

