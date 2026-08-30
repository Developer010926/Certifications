# Certifications

Система управления сотрудниками, контрактами и сертификациями.

## Структура репозитория

```text
Certifications/
├── Certifications.slnx      # Единый solution для backend-проектов
├── backend/                 # Проекты .NET 10 Minimal API
│   └── AGENTS.md            # Backend-инструкции для Codex
├── frontend/                # Angular CLI + Angular Material workspace
│   └── AGENTS.md            # Frontend-инструкции для Codex
├── .codex/config.toml       # Безопасные project-level настройки Codex
├── AGENTS.md                # Общие инструкции для Codex
├── Requirements.md          # Бизнес-требования
├── ApiDesign.md             # Архитектура API
├── UiDesign.md              # Архитектура UI
├── Certifications.xmi       # UML/XMI-модель
└── certifications.mdj       # Исходная StarUML-модель
```

Backend и frontend являются отдельными приложениями, но находятся в одном Git-репозитории и используют общий OpenAPI-контракт.

## Перед созданием проектов

1. Инициализировать Git в корне `Certifications/`, если репозиторий ещё не создан.
2. Открыть корень проекта в доверенном режиме Codex, чтобы загрузились `.codex/config.toml` и корневой `AGENTS.md`.
3. Для API открыть корневой `Certifications.slnx` в Rider.
4. Для UI открыть `frontend/` или Angular workspace в WebStorm.
5. Не сохранять реальные ключи в Git.

### Проверка локального окружения

На момент подготовки каркаса обнаружено:

```text
.NET SDK:    8.0.101
Angular CLI: 21.2.3
Node.js:     24.14.0
npm:         11.9.0
```

Перед созданием backend необходимо установить .NET 10 SDK и проверить `dotnet --version`. Angular-проект можно создавать установленным Angular CLI 21; `@angular/material` и `@angular/cdk` должны использовать совместимую major-версию 21.

## Целевая backend-структура

Backend-проекты подключаются к единому solution в корне репозитория:

```text
Certifications/
├── Certifications.slnx
└── backend/
    ├── Certifications.Api/
    ├── Certifications.Application/
    ├── Certifications.Domain/
    ├── Certifications.Infrastructure/
    └── Certifications.Tests/
```

Ориентировочные CLI-команды, если проект создаётся не через Rider:

```bash
dotnet new web -n Certifications.Api -f net10.0 -o backend/Certifications.Api
dotnet new classlib -n Certifications.Application -f net10.0 -o backend/Certifications.Application
dotnet new classlib -n Certifications.Domain -f net10.0 -o backend/Certifications.Domain
dotnet new classlib -n Certifications.Infrastructure -f net10.0 -o backend/Certifications.Infrastructure
dotnet new xunit -n Certifications.Tests -f net10.0 -o backend/Certifications.Tests

dotnet sln Certifications.slnx add \
  backend/Certifications.Api/Certifications.Api.csproj \
  backend/Certifications.Application/Certifications.Application.csproj \
  backend/Certifications.Domain/Certifications.Domain.csproj \
  backend/Certifications.Infrastructure/Certifications.Infrastructure.csproj \
  backend/Certifications.Tests/Certifications.Tests.csproj
```

После генерации необходимо настроить project references согласно `ApiDesign.md`.

## Целевая frontend-структура

После создания Angular workspace:

```text
frontend/
└── certifications-ui/
    ├── angular.json
    ├── package.json
    └── src/
```

Ориентировочные CLI-команды, если проект создаётся не через WebStorm:

```bash
ng new certifications-ui \
  --directory frontend/certifications-ui \
  --routing \
  --style scss \
  --standalone \
  --strict

cd frontend/certifications-ui
ng add @angular/material
```

Точную major-версию Angular и Angular Material следует выбрать до выполнения команд.

## Конфигурация секретов

В коммитируемом `appsettings.json` должны находиться пустые placeholders:

```json
{
  "Security": {
    "ApiKeyHeaderName": "X-API-Key",
    "ApiKey": "",
    "PasswordEncryptionKey": ""
  }
}
```

Реальные значения размещаются в игнорируемом environment-specific appsettings или передаются через environment variables.

## Запуск backend и PostgreSQL в Docker

Docker Compose запускает API и PostgreSQL 18. Данные PostgreSQL сохраняются в именованном volume `postgres-data`.

Сначала создайте локальный файл окружения и заполните все обязательные значения:

```bash
cp .env.example .env
```

Файл `.env` игнорируется Git, не должен добавляться в репозиторий и не должен содержать production-секреты. Для локального `POSTGRES_PASSWORD` используйте значение без `;`, поскольку пароль включается в строку подключения ASP.NET Core.

Соберите и запустите контейнеры:

```bash
docker compose up --build
```

В Windows можно запустить контейнеры в фоновом режиме корневым скриптом:

```bat
start-docker.cmd
```

В macOS тот же сценарий доступен через исполняемый shell-скрипт:

```bash
./start-docker.sh
```

Скрипты проверяют Docker и конфигурацию Compose, выводят строку подключения с замаскированным паролем, запускают сервисы через `docker compose up --build --detach` и показывают их состояние.

По умолчанию сервисы доступны на следующих портах:

- backend: `http://localhost:5081`;
- PostgreSQL: `localhost:5432`.

Порты можно переопределить переменными `BACKEND_PORT` и `POSTGRES_PORT` в `.env`. Backend получает строку подключения и секретные ключи через environment variables, которые переопределяют пустые placeholders в `appsettings.Development.json`.

Внутри Compose строка подключения использует `Host=postgres;Port=5432`. Для подключения к PostgreSQL с хост-машины, например из Rider, используйте `Host=localhost` и значение `POSTGRES_PORT` из `.env`.

При локальном запуске API вне Compose сохраните host-side строку подключения и секреты через .NET user secrets, не в `appsettings*.json`:

```bash
dotnet user-secrets set --project backend/Certifications.Api \
  "ConnectionStrings:DefaultConnection" \
  "Host=localhost;Port=5432;Database=<database>;Username=<username>;Password=<password>"
dotnet user-secrets set --project backend/Certifications.Api \
  "Security:ApiKey" "<api-key>"
dotnet user-secrets set --project backend/Certifications.Api \
  "Security:PasswordEncryptionKey" "<encryption-key>"
dotnet user-secrets set --project backend/Certifications.Api \
  "BootstrapAdmin:Password" "<initial-admin-password>"
```

`Security:ApiKey` must contain at least 32 characters. `Security:PasswordEncryptionKey`
must be a Base64-encoded 32-byte AES key; one can be generated with
`openssl rand -base64 32`. `BootstrapAdmin:Password` is used once to replace the
non-login seed marker for administrator `КП-0001`; it must contain at least eight
characters, including a letter and a digit. The plaintext bootstrap password is never
stored in PostgreSQL. Keep this value in a secret store and remove it from the runtime
environment after the first successful startup.

Every `/api/v1/*` request, including login, requires `X-API-Key`. Successful login
sets the secure `HttpOnly` cookie. Before a state-changing authenticated call, request
`GET /api/v1/auth/csrf-token` and send its `token` value in `X-CSRF-TOKEN`. Password
creation, generation, and reveal responses use `Cache-Control: no-store`.

Production is intended to be same-origin. Development CORS only permits the origins
listed in `Cors:AllowedOrigins` (Compose defaults to `http://localhost:4200`) and allows
credentials. The API key should be injected by a trusted same-origin reverse proxy; it
must not be compiled into a public Angular bundle.

### OpenAPI contract

Swashbuckle generates the API's OpenAPI 3 document and interactive Swagger UI. In the
Development environment, including the default Compose setup, browse to
`http://localhost:5081/swagger`; the runtime document is available at
`http://localhost:5081/swagger/v1/swagger.json`.

Building `Certifications.Api` also generates the checked backend contract at
`openapi/certifications-v1.json`. Endpoint names are stable `operationId` values, and
the document declares both the API-key and cookie-authentication requirements. When a
future Angular workspace is added, generate its client from this file and never edit
generated client sources manually. Swagger UI and its runtime JSON endpoint are
Development-only.

### Миграции базы данных

При обычном `docker compose up` отдельный одноразовый сервис сначала применяет проверенные EF Core migrations. Backend запускается только после его успешного завершения. Повторный запуск безопасен: уже применённые migrations пропускаются.

Для ручного повторного запуска сервиса migrations используйте:

```bash
docker compose run --rm --build migrations
```

`POSTGRES_PASSWORD` используется образом PostgreSQL только при создании базы. Если изменить пароль в `.env` после инициализации именованного volume, сохранённый пароль роли автоматически не изменится. Для пустой локальной базы можно пересоздать volume командой `docker compose down --volumes`; эта команда безвозвратно удаляет локальные данные PostgreSQL.

Миграции также добавляют демонстрационный набор из шести вымышленных сотрудников Департамента криминальной полиции. Пароли в migration хранятся как нерабочий безопасный маркер. При первом запуске API только администратор `КП-0001` получает зашифрованный bootstrap-пароль; пароли остальных демонстрационных сотрудников администратор может сгенерировать через REST API.

Для создания следующей миграции требуется установленный .NET 10 SDK. Из корня репозитория выполните:

```bash
dotnet tool restore
dotnet ef migrations add <MigrationName> \
  --project backend/Certifications.Infrastructure/Certifications.Infrastructure.csproj \
  --startup-project backend/Certifications.Api/Certifications.Api.csproj \
  --context CertificationsDbContext \
  --output-dir Persistence/Migrations
```

Перед commit проверьте синхронизацию модели и snapshot:

```bash
dotnet ef migrations has-pending-model-changes \
  --project backend/Certifications.Infrastructure/Certifications.Infrastructure.csproj \
  --startup-project backend/Certifications.Api/Certifications.Api.csproj \
  --context CertificationsDbContext
```

Остановить контейнеры, сохранив данные PostgreSQL:

```bash
docker compose down
```

Удаление volume командой `docker compose down --volumes` безвозвратно удалит локальные данные PostgreSQL.

## Работа с Codex

Codex читает инструкции иерархически:

- корневой `AGENTS.md` применяется ко всему репозиторию;
- `backend/AGENTS.md` уточняет правила API;
- `frontend/AGENTS.md` уточняет правила Angular.

Для задач, затрагивающих только одну часть, запускайте Codex из соответствующей папки. Для изменений OpenAPI, DTO и generated Angular client используйте корень репозитория, чтобы агент видел обе стороны контракта.
