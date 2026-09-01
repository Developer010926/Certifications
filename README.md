# Certifications

Веб-приложение для управления сотрудниками, контрактами и сертификациями. Система предоставляет отдельный пользовательский режим для просмотра собственных данных и административный режим для работы с сотрудниками, контрактами и этапами сертификации.

## Быстрый старт

Рекомендуемый способ локального запуска — Docker Compose:

```bash
cp .env.example .env
dotnet dev-certs https --trust
./start-docker.sh
```

В Windows используйте:

```bat
copy .env.example .env
dotnet dev-certs https --trust
start-docker.cmd
```

Перед запуском заполните обязательные значения в `.env`. Полная пошаговая инструкция приведена в [руководстве по началу работы](docs/GettingStarted.md).

После запуска:

- приложение: [https://localhost:4200](https://localhost:4200);
- Swagger UI: [http://localhost:5081/swagger](http://localhost:5081/swagger);
- PostgreSQL: `localhost:5432`.

## Документация

| Документ | Назначение |
| --- | --- |
| [Начало работы](docs/GettingStarted.md) | Подготовка окружения, настройка секретов, запуск и устранение типовых проблем |
| [Руководство пользователя](docs/UserGuide.md) | Вход, роли, сотрудники, контракты, сертификации и статусы |
| [Frontend README](frontend/certifications-ui/README.md) | Разработка, запуск, тестирование и генерация Angular API client |
| [Backend README](backend/README.md) | Архитектура API, конфигурация, миграции и тестирование |
| [Бизнес-требования](Requirements.md) | Функциональные требования и бизнес-правила |
| [Проектирование UI](UiDesign.md) | Маршруты, экраны и frontend-архитектура |
| [Проектирование API](ApiDesign.md) | REST API, доменная модель и backend-архитектура |
| [OpenAPI-контракт](openapi/certifications-v1.json) | Контракт для генерации типизированного Angular client |

## Структура репозитория

```text
Certifications/
├── backend/                       # .NET 10 Minimal API
├── frontend/certifications-ui/    # Angular 21 + Angular Material
├── docs/                          # Документация пользователя и разработчика
├── openapi/                       # Генерируемый OpenAPI-контракт
├── compose.yaml                   # PostgreSQL, migrations, API и frontend
├── Certifications.slnx            # Solution для Rider и .NET CLI
├── Requirements.md                # Бизнес-требования
├── ApiDesign.md                   # Проектирование backend/API
└── UiDesign.md                    # Проектирование frontend/UI
```

Backend и frontend являются отдельными приложениями, но используют один Git-репозиторий и общий OpenAPI-контракт.

## Технологии

- .NET 10, ASP.NET Core Minimal API;
- Entity Framework Core 10 и PostgreSQL 18;
- Angular 21, Angular Material и TypeScript;
- Orval для генерации Angular client из OpenAPI;
- secure `HttpOnly` cookie для пользовательской сессии;
- CSRF token для изменяющих запросов;
- `X-API-Key` как дополнительная защита канала API;
- Docker Compose и Nginx для локального интеграционного запуска.

## Разработка в IDE

- В Rider откройте `Certifications.slnx`.
- В WebStorm откройте `frontend/certifications-ui` как Angular-проект.
- Корневые требования и модели остаются общими для обеих частей системы.

## Основные команды проверки

Backend:

```bash
dotnet build Certifications.slnx
dotnet test Certifications.slnx
dotnet format Certifications.slnx --verify-no-changes
```

Frontend:

```bash
cd frontend/certifications-ui
npm ci
npm run api:generate
npm test -- --watch=false
npm run build
```

## Безопасность

Не добавляйте в Git `.env`, реальные строки подключения, API key, ключ шифрования паролей, bootstrap-пароль, TLS private key или раскрытые пользовательские пароли. Angular bundle не должен содержать `X-API-Key`: при локальном запуске заголовок добавляет dev proxy или Nginx.

## Остановка Docker Compose

Остановить приложение, сохранив данные PostgreSQL:

```bash
docker compose down
```

Удалить контейнеры вместе с локальной базой данных:

```bash
docker compose down --volumes
```

Вторая команда безвозвратно удаляет локальные данные из именованного volume.
