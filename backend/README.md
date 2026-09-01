# Certifications Backend

Backend системы управления сотрудниками, контрактами и сертификациями. Реализован на .NET 10 как ASP.NET Core Minimal API с PostgreSQL и Entity Framework Core 10.

## Структура solution

```text
backend/
├── Certifications.Api             # HTTP, auth, OpenAPI, middleware и endpoints
├── Certifications.Application     # сценарии, DTO, валидация и сервисы
├── Certifications.Domain          # сущности, enum и бизнес-правила
├── Certifications.Infrastructure  # EF Core, PostgreSQL, migrations и security
└── Certifications.Tests           # unit и integration tests
```

Архитектурные решения подробно описаны в [ApiDesign.md](../ApiDesign.md).

## Требования к окружению

- .NET 10 SDK;
- PostgreSQL 18 или совместимый поддерживаемый PostgreSQL;
- восстановленный локальный tool manifest для `dotnet-ef`;
- доверенный ASP.NET Core HTTPS development certificate для совместной работы с frontend.

Проверка SDK и восстановление tools:

```bash
dotnet --version
dotnet tool restore
```

## Конфигурация секретов

Не записывайте реальные значения в `appsettings.json` или `appsettings.Development.json`. Для локального запуска вне Docker используйте .NET User Secrets:

```bash
dotnet user-secrets set --project backend/Certifications.Api \
  "ConnectionStrings:DefaultConnection" \
  "Host=localhost;Port=5432;Database=<database>;Username=<username>;Password=<password>"

dotnet user-secrets set --project backend/Certifications.Api \
  "Security:ApiKey" "<api-key-minimum-32-characters>"

dotnet user-secrets set --project backend/Certifications.Api \
  "Security:PasswordEncryptionKey" "<base64-encoded-32-byte-key>"

dotnet user-secrets set --project backend/Certifications.Api \
  "BootstrapAdmin:Password" "<initial-admin-password>"
```

Создать AES key:

```bash
openssl rand -base64 32
```

Bootstrap-пароль должен содержать не менее восьми символов, букву и цифру. Специальные символы разрешены, но не обязательны.

## База данных и migrations

Применить существующие migrations:

```bash
dotnet tool run dotnet-ef database update \
  --project backend/Certifications.Infrastructure/Certifications.Infrastructure.csproj \
  --startup-project backend/Certifications.Api/Certifications.Api.csproj \
  --context CertificationsDbContext
```

Создать новую migration после согласованного изменения модели:

```bash
dotnet tool run dotnet-ef migrations add <MigrationName> \
  --project backend/Certifications.Infrastructure/Certifications.Infrastructure.csproj \
  --startup-project backend/Certifications.Api/Certifications.Api.csproj \
  --context CertificationsDbContext \
  --output-dir Persistence/Migrations
```

Проверить наличие несохранённых изменений модели:

```bash
dotnet tool run dotnet-ef migrations has-pending-model-changes \
  --project backend/Certifications.Infrastructure/Certifications.Infrastructure.csproj \
  --startup-project backend/Certifications.Api/Certifications.Api.csproj \
  --context CertificationsDbContext
```

Не применяйте migrations к пользовательской или production-базе без отдельного подтверждения.

## Локальный запуск API

Доверьте development certificate:

```bash
dotnet dev-certs https --trust
```

Запустите HTTPS-профиль:

```bash
dotnet run --project backend/Certifications.Api --launch-profile https
```

Адреса по умолчанию:

- HTTPS API: `https://localhost:7055`;
- HTTP API: `http://localhost:5081`;
- Swagger UI: `https://localhost:7055/swagger`;
- runtime Swagger JSON: `https://localhost:7055/swagger/v1/swagger.json`.

Базовый маршрут API: `/api/v1`.

## Аутентификация и защита запросов

- Каждый маршрут `/api/v1/*`, включая login, требует `X-API-Key`.
- После успешного входа API устанавливает secure `HttpOnly` cookie.
- Изменяющие аутентифицированные запросы требуют `X-CSRF-TOKEN`.
- CSRF token выдаётся endpoint `GET /api/v1/auth/csrf-token`.
- Admin и active-contract access проверяются на сервере для каждого защищённого сценария.
- Ответы генерации и раскрытия паролей используют `Cache-Control: no-store`.

API key является дополнительной защитой канала, а не пользовательской аутентификацией.

## OpenAPI и Angular client

В Development доступен Swagger UI и runtime Swagger JSON. Сборка API также создаёт контракт:

```text
openapi/certifications-v1.json
```

Angular client генерируется из этого файла через Orval:

```bash
cd frontend/certifications-ui
npm run api:generate
```

Endpoint names задают стабильные OpenAPI `operationId`. При изменении DTO или endpoint сначала обновите OpenAPI, затем перегенерируйте frontend client. Генерируемые TypeScript-файлы нельзя править вручную.

## Сборка и тестирование

Из корня репозитория:

```bash
dotnet restore Certifications.slnx
dotnet build Certifications.slnx
dotnet test Certifications.slnx
dotnet format Certifications.slnx --verify-no-changes
```

Для быстрой проверки только тестового проекта:

```bash
dotnet test backend/Certifications.Tests/Certifications.Tests.csproj
```

Integration tests поднимают тестовое API и проверяют REST-контракт, security metadata и основные бизнес-сценарии.

## Docker Compose

Полный стек запускается из корня репозитория. Одноразовый сервис `migrations` применяет EF Core migrations до запуска backend, а PostgreSQL сохраняет данные в именованном volume.

```bash
./start-docker.sh
```

Ручной повторный запуск migrations service:

```bash
docker compose run --rm --build migrations
```

Подробнее: [Начало работы](../docs/GettingStarted.md).
