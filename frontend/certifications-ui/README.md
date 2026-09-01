# Certifications UI

Frontend системы управления сотрудниками, контрактами и сертификациями. Приложение реализовано на Angular 21 с Angular Material, standalone components, Reactive Forms и типизированным API client, сгенерированным Orval.

## Требования к окружению

- Node.js 24;
- npm 11;
- Angular CLI 21 через локальную зависимость проекта;
- .NET 10 SDK для подготовки доверенного localhost-сертификата;
- запущенный Certifications API;
- заполненный корневой файл `.env` с `SECURITY_API_KEY`.

Для полной подготовки системы используйте [общее руководство по началу работы](../../docs/GettingStarted.md).

## Установка зависимостей

Из каталога `frontend/certifications-ui` выполните:

```bash
npm ci
```

## Генерация API client

Orval генерирует Angular client из канонического контракта:

```text
../../openapi/certifications-v1.json
```

Конфигурация находится в `orval.config.ts`, результат — в `src/app/core/api/generated`. Генерируемые файлы нельзя редактировать вручную.

Однократная генерация:

```bash
npm run api:generate
```

Отслеживание изменений контракта:

```bash
npm run api:watch
```

После изменения backend-контракта сначала пересоберите API, затем повторно запустите генерацию client.

## Локальная конфигурация

Браузер обращается к относительным адресам `/api/v1/*`. Angular dev proxy пересылает `/api` на `https://localhost:7055` и добавляет `X-API-Key` вне browser bundle.

В корне репозитория создайте локальный `.env`:

```bash
cp .env.example .env
```

Значение `SECURITY_API_KEY` должно совпадать с ключом API. Файл `.env` игнорируется Git.

Один раз доверьте ASP.NET Core HTTPS-сертификат:

```bash
dotnet dev-certs https --trust
```

## Запуск для разработки

Сначала запустите backend с HTTPS-профилем. Затем из Angular-проекта выполните:

```bash
npm start
```

Команда `prestart` подготавливает сертификат в игнорируемом каталоге `.certificates`, после чего Angular открывается по адресу:

[https://localhost:4200](https://localhost:4200)

HTTPS необходим для secure authentication и antiforgery cookies. Проверка backend-сертификата в proxy остаётся включённой.

## Сборка и тестирование

Production build:

```bash
npm run build
```

Unit tests в однократном режиме:

```bash
npm test -- --watch=false
```

Интерактивный режим тестов:

```bash
npm test
```

Непрерывная development-сборка:

```bash
npm run watch
```

В проекте пока нет отдельного e2e runner и npm-скрипта lint; не используйте `ng e2e` или `npm run lint`, пока они не будут настроены.

## Структура приложения

```text
src/app/
├── core/       # auth, guards, interceptors, error handling, generated API
├── features/   # login, сотрудники, контракты, сертификации, личная страница
├── layout/     # оболочка и навигация
└── shared/     # общие компоненты, формы, статусы и утилиты
```

Основные маршруты:

```text
/login
/select-mode
/me
/admin/certifications
/admin/users
/admin/users/new
/admin/users/:employeeId
/admin/users/:employeeId/contract
/admin/certifications/:certificationId
```

Пользовательские сценарии описаны в [руководстве пользователя](../../docs/UserGuide.md), архитектурные решения — в [UiDesign.md](../../UiDesign.md).

## Работа с API и сессией

- Все запросы отправляются с `withCredentials: true`.
- Содержимое `HttpOnly` cookie недоступно Angular-коду.
- CSRF token хранится только в памяти и передаётся в `X-CSRF-TOKEN` для изменяющих запросов.
- API key не хранится в Angular source, environment-файлах или browser storage.
- Раскрытый пароль существует только в памяти диалога и очищается при его закрытии.
- Статус контракта и `EffectiveValidTo` вычисляет backend.

## Docker

Production-сборка frontend создаётся в Node-контейнере и обслуживается Nginx по HTTPS. Nginx добавляет API key во время выполнения и проксирует `/api/v1/*` в backend по внутренней Docker-сети.

Запускайте весь стек из корня репозитория:

```bash
./start-docker.sh
```

В Windows:

```bat
start-docker.cmd
```

## WebStorm

Откройте в WebStorm именно каталог:

```text
frontend/certifications-ui
```

WebStorm распознает `angular.json`, `package.json`, TypeScript и Angular Material. Инструкции Codex для этого проекта находятся в `AGENTS.md` и `.codex/config.toml`.
