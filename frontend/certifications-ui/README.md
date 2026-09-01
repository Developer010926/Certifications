# Certifications UI

This project was generated using [Angular CLI](https://github.com/angular/angular-cli) version 21.2.3.

## API client generation

The typed Angular client is generated with Orval 8.27.0 exclusively from the canonical contract at `../../openapi/certifications-v1.json`. Generated files are isolated under `src/app/core/api/generated` and must not be edited manually.

```bash
npm run api:generate
```

For contract development, use `npm run api:watch`.

## Local development setup

The browser calls relative `/api/v1/*` paths. The Angular development proxy forwards `/api` to `https://localhost:7055` and injects `X-API-Key` outside the browser bundle.

The one local configuration step is to copy the repository-root `.env.example` to `.env` and set `SECURITY_API_KEY` to the same value used by the backend. The `.env` file is ignored by Git.

Trust the ASP.NET Core HTTPS development certificate once:

```bash
dotnet dev-certs https --trust
```

The `npm start` command verifies the certificate trust, exports the certificate and key into the ignored local `.certificates` directory, and serves Angular over HTTPS. The private key is restricted to the current user and is never committed or included in the browser bundle.

Angular launches through Node with `--use-system-ca`. On macOS, the proxy also loads the public `localhost` certificates from the user's Keychain because Node does not consistently include trusted ASP.NET leaf certificates in its system CA list. Backend certificate validation remains enabled with `secure: true`.

## Development server

After trusting the certificate, restart the backend and start the local development server with:

```bash
npm start
```

Once the server is running, open your browser and navigate to `https://localhost:4200/`. Using HTTPS is required so the backend's `Secure` authentication and antiforgery cookies remain available after login. The application will automatically reload whenever you modify any of the source files.

## Code scaffolding

Angular CLI includes powerful code scaffolding tools. To generate a new component, run:

```bash
ng generate component component-name
```

For a complete list of available schematics (such as `components`, `directives`, or `pipes`), run:

```bash
ng generate --help
```

## Building

To build the project run:

```bash
ng build
```

This will compile your project and store the build artifacts in the `dist/` directory. By default, the production build optimizes your application for performance and speed.

## Running unit tests

To execute unit tests with the [Vitest](https://vitest.dev/) test runner, use the following command:

```bash
ng test
```

## Running end-to-end tests

For end-to-end (e2e) testing, run:

```bash
ng e2e
```

Angular CLI does not come with an end-to-end testing framework by default. You can choose one that suits your needs.

## Additional Resources

For more information on using the Angular CLI, including detailed command references, visit the [Angular CLI Overview and Command Reference](https://angular.dev/tools/cli) page.
