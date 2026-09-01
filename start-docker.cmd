@echo off
setlocal EnableExtensions DisableDelayedExpansion

cd /d "%~dp0"
if errorlevel 1 (
  echo ERROR: Cannot switch to the project directory. 1>&2
  exit /b 1
)

where docker >nul 2>&1
if errorlevel 1 (
  echo ERROR: Docker CLI was not found in PATH. 1>&2
  exit /b 1
)

docker info >nul 2>&1
if errorlevel 1 (
  echo ERROR: Docker is not running. 1>&2
  exit /b 1
)

where node >nul 2>&1
if errorlevel 1 (
  echo ERROR: Node.js was not found in PATH. 1>&2
  exit /b 1
)

where dotnet >nul 2>&1
if errorlevel 1 (
  echo ERROR: .NET SDK was not found in PATH. 1>&2
  exit /b 1
)

if not exist ".env" (
  echo ERROR: .env was not found. Create it with: copy .env.example .env 1>&2
  exit /b 1
)

node frontend\certifications-ui\scripts\prepare-dev-https.mjs
if errorlevel 1 (
  echo ERROR: The trusted localhost HTTPS certificate could not be prepared. 1>&2
  exit /b 1
)

for /f "usebackq eol=# tokens=1,* delims==" %%A in (".env") do set "%%A=%%B"

if not defined POSTGRES_PORT set "POSTGRES_PORT=5432"
if not defined BACKEND_PORT set "BACKEND_PORT=5081"
if not defined FRONTEND_PORT set "FRONTEND_PORT=4200"

docker compose config --quiet
if errorlevel 1 (
  echo ERROR: Docker Compose configuration is invalid. 1>&2
  exit /b 1
)

echo Connection string: Host=localhost;Port=%POSTGRES_PORT%;Database=%POSTGRES_DB%;Username=%POSTGRES_USER%;Password=^<redacted^>
echo Backend: http://localhost:%BACKEND_PORT%
echo Frontend: https://localhost:%FRONTEND_PORT%

docker compose up --build --detach
if errorlevel 1 (
  echo ERROR: Docker Compose failed to start. 1>&2
  exit /b 1
)

docker compose ps
if errorlevel 1 exit /b 1

exit /b 0
