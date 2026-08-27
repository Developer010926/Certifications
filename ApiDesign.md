# Проектирование REST API

## 1. Назначение документа

Документ описывает рекомендуемую архитектуру backend-приложения системы управления контрактами и сертификациями.

Утверждённый технологический стек:

- .NET 10;
- ASP.NET Core Minimal API;
- PostgreSQL;
- Entity Framework Core 10 с PostgreSQL provider;
- REST/JSON;
- OpenAPI как контракт между backend и frontend;
- API key как дополнительная защита HTTP API;
- защищённая `HttpOnly` cookie для пользовательской аутентификации после логина.

Бизнес-требования определены в [Requirements.md](./Requirements.md).

## 2. Архитектурный подход

Backend рекомендуется разделить на логические проекты:

```text
Certifications.Api
├── конфигурация ASP.NET Core
├── группы Minimal API endpoints
├── authentication/authorization
├── обработка ошибок
└── OpenAPI

Certifications.Application
├── сценарии сотрудников
├── сценарии контрактов
├── сценарии сертификаций
├── расчёт статусов
└── DTO и валидация сценариев

Certifications.Domain
├── сущности
├── перечисления
├── бизнес-правила
└── доменные операции

Certifications.Infrastructure
├── доступ к базе данных
├── миграции
├── шифрование паролей
└── реализация репозиториев/адаптеров

Certifications.Tests
├── unit-тесты домена
├── unit-тесты Application
└── integration-тесты API
```

Minimal API используется только как HTTP-граница. Бизнес-правила не должны размещаться непосредственно в `Program.cs` или route handlers.

## 3. Реализационная доменная модель

### 3.1. Employee

```text
Employee
├── Id: Guid | long
├── PersonalId: string
├── NormalizedPersonalId: string
├── FirstName: string
├── MiddleName: string?
├── LastName: string
├── EncryptedPassword: string
├── IsAdmin: bool
├── PreferredAdminMode: AdminMode?
└── Contracts: Contract[]
```

Требуемые изменения относительно XMI:

- добавить неизменяемый технический ключ `Employee.Id`;
- не использовать изменяемый `PersonalId` как primary key;
- оставить `PersonalId` уникальным пользовательским логином;
- добавить `NormalizedPersonalId` и уникальный индекс;
- переименовать `PasswordHash` в `EncryptedPassword`;
- добавить `PreferredAdminMode` для запоминания последнего выбора администратора.

Нормализация `PersonalId` выполняется одинаково при создании, изменении и входе:

```text
NormalizedPersonalId = RemoveWhitespace(PersonalId).ToUpperInvariant()
```

### 3.2. Contract

```text
Contract
├── Id: long
├── EmployeeId: Employee.Id
├── Position: string
├── Department: string?
├── Division: string?
├── ContractDate: DateOnly
├── ValidTo: DateOnly?
├── Active: bool
├── ProlongationWarningMonths: int = 3
├── ProlongationAlertMonths: int = 1
├── ProlongationForYears: int = 1
├── RowVersion: concurrency token
└── Prolongations: Prolongation[]
```

Для календарных дат рекомендуется `DateOnly`, поскольку требования не используют время суток или часовой пояс.

Ограничения:

- у сотрудника не более одного активного контракта;
- `ProlongationAlertMonths < ProlongationWarningMonths`;
- месячные пороги неотрицательны;
- `ProlongationForYears > 0`;
- новый активный контракт создаётся только после закрытия предыдущего.

Ограничение одного активного контракта должно проверяться Application-слоем и, если выбранная СУБД поддерживает это, уникальным частичным/фильтрованным индексом.

### 3.3. Prolongation

```text
Prolongation
├── Id: long
├── ContractId: Contract.Id
├── Assessor: string
├── CertificationDate: DateOnly
├── ProtocolDate: DateOnly?
├── ProlongationSend: DateOnly?
└── ProlongationReturned: DateOnly?
```

Требуемые изменения относительно XMI:

- переименовать `Accessor` в `Assessor`;
- сделать `CertificationDate` обязательным;
- явно добавить `ContractId`.

Порядок дат:

```text
CertificationDate <= ProtocolDate
ProtocolDate      <= ProlongationSend
ProlongationSend  <= ProlongationReturned
```

Нельзя заполнить последующий этап без предыдущего.

### 3.4. CertificationStatus

Статус является доменным/прикладным результатом, а не UI-типом:

```text
NotApplicable
ContractValid
CertificationPending
CertificationInProgress
CertificationMissing
```

Статус рекомендуется вычислять на backend при чтении, а не хранить как независимо изменяемое поле.

## 4. Доменные сервисы

### 4.1. Расчёт EffectiveValidTo

```text
EffectiveValidTo = Contract.ValidTo
                   если ValidTo заполнено

EffectiveValidTo = Contract.ContractDate
                   + Contract.ProlongationForYears
                   если ValidTo не заполнено
```

### 4.2. Расчёт статуса

```text
1. Нет активного контракта
   → NotApplicable

2. Есть незавершённая последняя сертификация
   → CertificationInProgress

3. Today < EffectiveValidTo - WarningMonths
   → ContractValid

4. Today >= EffectiveValidTo - WarningMonths
   и Today < EffectiveValidTo - AlertMonths
   → CertificationPending

5. Today >= EffectiveValidTo - AlertMonths
   и незавершённой сертификации нет
   → CertificationMissing
```

`CertificationInProgress` имеет наивысший приоритет среди статусов активного контракта.

### 4.3. Завершение сертификации

Операция завершения выполняется одной транзакцией:

1. Проверить, что сертификация ещё не завершена.
2. Проверить заполнение и порядок всех дат.
3. Установить `ProlongationReturned`.
4. Рассчитать `Contract.ValidTo = ProtocolDate + ProlongationForYears`.
5. Сохранить сертификацию и контракт.
6. После commit возвращать `ContractValid`.

Завершённая сертификация неизменяема и не может быть возвращена в работу.

## 5. REST-ресурсы

Базовый префикс API:

```text
/api/v1
```

### 5.1. Authentication и текущий пользователь

```http
POST /api/v1/auth/login
POST /api/v1/auth/logout
GET  /api/v1/auth/me
PUT  /api/v1/auth/preferred-mode
```

`POST /auth/login` принимает `PersonalId` и пароль, нормализует `PersonalId`, проверяет пароль и наличие активного контракта. При успешном входе API создаёт пользовательскую сессию и устанавливает защищённую `HttpOnly` cookie. Пароль и пользовательские данные не помещаются в cookie.

`POST /auth/logout` завершает сессию и удаляет authentication cookie.

`GET /auth/me` возвращает минимальную информацию для frontend:

```json
{
  "employeeId": "...",
  "personalId": "EMP001",
  "displayName": "Имя Фамилия",
  "isAdmin": true,
  "preferredAdminMode": "Administration"
}
```

### 5.2. Employees

```http
GET   /api/v1/employees
GET   /api/v1/employees/{employeeId}
POST  /api/v1/employees
PATCH /api/v1/employees/{employeeId}
```

`POST /employees` атомарно создаёт сотрудника и его первый контракт.

Физический `DELETE /employees/{id}` не предоставляется.

### 5.3. Password operations

```http
POST /api/v1/employees/{employeeId}/password/generate
POST /api/v1/employees/{employeeId}/password/reveal
POST /api/v1/me/password/reveal
```

Операции раскрытия пароля:

- требуют явной авторизации;
- не возвращаются в обычных Employee DTO;
- должны отправлять заголовок `Cache-Control: no-store`;
- не должны записывать пароль в application logs;
- не должны включать пароль в URL, query string или exception details.

### 5.4. Contracts

```http
GET  /api/v1/employees/{employeeId}/contracts/current
POST /api/v1/employees/{employeeId}/contracts
POST /api/v1/contracts/{contractId}/close
GET  /api/v1/me/contract
```

История контрактов хранится, но отдельный публичный UI/API use case для неё не требуется. Infrastructure-слой всё равно сохраняет архивные записи.

### 5.5. Certifications

```http
GET   /api/v1/contracts/{contractId}/certifications
POST  /api/v1/contracts/{contractId}/certifications
PATCH /api/v1/certifications/{certificationId}
POST  /api/v1/certifications/{certificationId}/return
```

`PATCH` разрешён только до заполнения `ProlongationReturned`.

Отдельный action endpoint `/return` используется потому, что возврат сертификации является бизнес-операцией с транзакционным изменением нескольких сущностей.

### 5.6. Certification overview

```http
GET /api/v1/certifications/overview
```

Поддерживаемые query parameters:

```text
page
pageSize
name
department
status
validToFrom
validToTo
includeInactive
sort
direction
```

Пример:

```http
GET /api/v1/certifications/overview?page=1&pageSize=25&status=CertificationPending&sort=effectiveValidTo&direction=asc
```

Ответ:

```json
{
  "items": [],
  "page": 1,
  "pageSize": 25,
  "totalCount": 0
}
```

Фильтрация, сортировка и пагинация выполняются на сервере.

## 6. DTO

REST API не должен сериализовать database/domain entities напрямую.

Рекомендуемые DTO:

- `LoginRequest`;
- `CurrentUserDto`;
- `CreateEmployeeRequest`;
- `UpdateEmployeeRequest`;
- `EmployeeSummaryDto`;
- `EmployeeDetailsDto`;
- `CreateContractRequest`;
- `ContractDto`;
- `CreateCertificationRequest`;
- `UpdateCertificationRequest`;
- `CertificationDto`;
- `CertificationOverviewRowDto`;
- `PagedResult<T>`;
- `ValidationProblemDetails`.

`CertificationOverviewRowDto` агрегирует Employee, активный Contract, последнюю Prolongation, `EffectiveValidTo` и вычисленный `CertificationStatus`.

`EncryptedPassword` не включается ни в один обычный DTO.

## 7. Валидация и HTTP-ошибки

Рекомендуемое соответствие:

| Ситуация | HTTP status |
|---|---:|
| Успешное чтение | `200 OK` |
| Успешное создание | `201 Created` |
| Успешная команда без тела | `204 No Content` |
| Ошибка входных данных | `400 Bad Request` |
| Ошибка аутентификации | `401 Unauthorized` |
| Недостаточно прав | `403 Forbidden` |
| Ресурс не найден | `404 Not Found` |
| Конфликт `PersonalId`, активного контракта или незавершённой сертификации | `409 Conflict` |
| Конфликт конкурентного изменения | `409 Conflict` или `412 Precondition Failed` |

Ошибки возвращаются в едином формате `ProblemDetails`/`ValidationProblemDetails`.

## 8. Authentication и authorization

Используется двухуровневая схема:

1. API key проверяет, что запрос пришёл через разрешённый клиентский канал.
2. `HttpOnly` secure cookie идентифицирует вошедшего пользователя и используется для проверки его прав.

API key является дополнительным барьером и не заменяет пользовательскую аутентификацию. Один API key не может определить Employee, `IsAdmin` или право доступа к `/me`.

Рекомендуемый порядок обработки запроса:

```text
HTTPS
→ проверка X-API-Key
→ cookie authentication
→ проверка активного контракта
→ authorization policy
→ endpoint
```

API key:

- передаётся в заголовке `X-API-Key`;
- не передаётся в URL или query string;
- сравнивается на backend безопасным способом;
- не записывается в logs;
- применяется ко всем `/api/v1/*` endpoints, включая login, если явно не задано исключение;
- предпочтительно добавляется reverse proxy/gateway, чтобы не встраивать секрет в публичный Angular bundle.

Если Angular отправляет API key непосредственно из браузера, значение можно извлечь из frontend-кода. В таком режиме API key считается только дополнительным идентификатором/фильтром запросов, но не самостоятельным секретом безопасности.

Authentication cookie должна иметь как минимум:

```text
HttpOnly = true
Secure = true
SameSite = Lax или Strict при совместимом размещении
```

Cookie хранит только защищённый authentication ticket с техническим `Employee.Id` и минимально необходимыми claims. Актуальные `IsAdmin` и наличие активного контракта backend проверяет по текущим данным, чтобы снятие административных прав или закрытие контракта вступало в силу без задержки.

Политики доступа:

```text
Authenticated
AdminOnly
OwnEmployeeData
ActiveContractRequired
```

Backend является окончательной точкой контроля доступа. Angular guards используются только для UX и не заменяют серверную авторизацию.

Для изменяющих состояние запросов необходима защита от CSRF. При разных origin требуется явная конфигурация CORS с разрешением credentials только для утверждённого frontend origin.

### 8.1. Конфигурация appsettings

API key и ключ шифрования паролей загружаются через configuration system ASP.NET Core из `appsettings`:

```json
{
  "Security": {
    "ApiKeyHeaderName": "X-API-Key",
    "ApiKey": "",
    "PasswordEncryptionKey": ""
  },
  "Authentication": {
    "CookieName": "Certifications.Auth",
    "ExpireMinutes": 480
  }
}
```

В репозиторий разрешено сохранять структуру и пустые placeholders. Реальные production-значения должны находиться в environment-specific `appsettings` вне Git либо передаваться через environment variables, которые переопределяют соответствующие настройки. API не должен запускаться, если обязательные ключи отсутствуют или имеют недопустимый формат.

## 9. Шифрование паролей

Для хранения используется authenticated encryption. Ключ:

- не хранится в таблице Employees;
- не возвращается API;
- не отображается в UI;
- загружается из `Security:PasswordEncryptionKey` через ASP.NET Core configuration;
- должен иметь определённую процедуру резервного копирования и ротации.

Генерация нового пароля заменяет единственное актуальное зашифрованное значение, поэтому старый пароль немедленно становится недействительным.

## 10. Persistence

Утверждённая СУБД — PostgreSQL. Доступ к данным выполняется через Entity Framework Core 10 и PostgreSQL provider.

Изменения схемы выполняются через EF Core migrations. Миграции должны храниться в Infrastructure-проекте и применяться контролируемо при развёртывании.

Необходимые индексы:

- unique index на `Employee.NormalizedPersonalId`;
- index на `Contract.EmployeeId`;
- index для поиска активного контракта;
- partial unique index, предотвращающий два активных контракта одного сотрудника, например по `EmployeeId` с условием `WHERE Active = TRUE`;
- index на `(Prolongation.ContractId, CertificationDate DESC)`;
- индексы для серверной фильтрации по подразделению и срокам.

## 11. OpenAPI и Angular client

Каждый endpoint должен иметь:

- стабильный `operationId` через `WithName`;
- описание request/response DTO;
- перечень возможных status codes;
- описание authorization requirements.

OpenAPI-документ используется для генерации типизированного TypeScript-клиента Angular. Это позволяет backend и frontend разрабатывать независимо по зафиксированному контракту.

Официальная документация:

- [ASP.NET Core Minimal APIs](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis?view=aspnetcore-10.0)
- [OpenAPI metadata in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/include-metadata?view=aspnetcore-10.0)

## 12. Тестирование

Обязательные unit-тесты:

- нормализация `PersonalId`;
- все границы расчёта статусов;
- приоритет `CertificationInProgress`;
- расчёт `EffectiveValidTo`;
- валидация порогов;
- порядок дат сертификации;
- формула продления.

Обязательные integration-тесты:

- атомарное создание Employee + Contract;
- запрет второго активного контракта;
- запрет второй незавершённой сертификации;
- транзакционное завершение сертификации;
- блокировка завершённой сертификации;
- права администратора и обычного пользователя;
- запрет входа без активного контракта;
- защита password endpoints.

## 13. Незакрытые технические решения

До начала реализации необходимо утвердить:

1. Способ генерации Angular client из OpenAPI.
2. Модель размещения Angular и API: один origin или разные origin.
3. Конкретный reverse proxy/gateway, который будет добавлять `X-API-Key`, либо подтверждение, что видимость дополнительного API key в браузере принимается.
4. Физическое расположение production `appsettings` и процедура безопасной доставки/ротации ключей.
