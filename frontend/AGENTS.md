# Frontend instructions

These instructions extend the repository-level `AGENTS.md` for work under `frontend/`.

## Technology

- Use Angular created and maintained with Angular CLI.
- Use Angular Material and CDK with the same major version as Angular.
- Use standalone components, lazy-loaded feature routes, Reactive Forms, and TypeScript strict mode.
- Follow `../UiDesign.md` for routes, screens, and UI behavior.

## Architecture

- Organize code into `core`, `shared`, `features`, and `layout` areas.
- Use a generated TypeScript client from backend OpenAPI.
- Do not manually edit generated API models or services.
- Keep business status calculation on the backend; display the returned status and `EffectiveValidTo`.
- Use server-side filtering, sorting, and pagination for employee and certification tables.
- Start with Angular services plus signals/RxJS; do not add a global state library without demonstrated need.

## Authentication and security

- Send API requests with credentials so the browser can attach the `HttpOnly` authentication cookie.
- The UI must never attempt to read the authentication cookie.
- Treat the API key as an additional channel marker only. Prefer reverse-proxy injection of `X-API-Key`.
- Never store passwords, encryption keys, or API keys in `localStorage`, `sessionStorage`, URLs, analytics events, or logs.
- Keep a revealed password only in local component state and clear it when the dialog closes.
- Route guards improve UX but do not replace backend authorization.

## UI conventions

- Use Angular Material components before creating custom controls.
- Use Reactive Forms for all editable forms.
- Show API field validation next to the corresponding control.
- Confirm irreversible actions such as certification return and contract close.
- Do not rely on color alone for certification status; include text and accessible labels.
- Preserve keyboard navigation, focus management, and responsive table behavior.

## Verification

After the Angular workspace exists, run the narrowest applicable commands defined in `package.json`, normally:

```bash
npm run lint
npm test -- --watch=false
npm run build
```

Do not invent a script that is not present in `package.json`; inspect available scripts first.

