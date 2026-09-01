# Начало работы

Это руководство описывает подготовку локального окружения и первый запуск всей системы Certifications.

## 1. Предварительные требования

Установите:

- Git;
- Docker Desktop или Docker Engine с Compose;
- Node.js 24;
- .NET 10 SDK.

Проверьте окружение:

```bash
docker --version
docker compose version
node --version
npm --version
dotnet --version
```

## 2. Подготовка `.env`

Из корня репозитория создайте локальный файл:

```bash
cp .env.example .env
```

В Windows:

```bat
copy .env.example .env
```

Заполните обязательные значения:

```dotenv
POSTGRES_DB=certifications
POSTGRES_USER=certifications
POSTGRES_PASSWORD=<local-database-password>
SECURITY_API_KEY=<api-key>
PASSWORD_ENCRYPTION_KEY=<base64-aes-key>
BOOTSTRAP_ADMIN_PASSWORD=<initial-admin-password>
```

Дополнительные значения уже имеют локальные defaults:

```dotenv
BOOTSTRAP_ADMIN_PERSONAL_ID=КП-0001
BUSINESS_TIME_ZONE=Europe/Vienna
FRONTEND_ORIGIN=https://localhost:4200
FRONTEND_PORT=4200
BACKEND_PORT=5081
POSTGRES_PORT=5432
```

Сгенерировать значения можно так:

```bash
openssl rand -hex 32
openssl rand -base64 32
```

Первую команду используйте для `SECURITY_API_KEY`, вторую — для `PASSWORD_ENCRYPTION_KEY`.

Требования к bootstrap-паролю:

- минимум восемь символов;
- хотя бы одна буква;
- хотя бы одна цифра;
- специальные символы разрешены, но не обязательны.

Не добавляйте `.env` в Git и не используйте production-секреты в локальном окружении.

## 3. Доверенный HTTPS-сертификат

Один раз выполните:

```bash
dotnet dev-certs https --trust
```

После подтверждения перезапустите терминал и браузер, если они были открыты до установки сертификата.

## 4. Запуск через Docker Compose

macOS/Linux:

```bash
./start-docker.sh
```

Windows:

```bat
start-docker.cmd
```

Скрипт:

1. проверяет Docker, Node.js и .NET SDK;
2. экспортирует доверенный localhost-сертификат;
3. проверяет Compose-конфигурацию;
4. собирает контейнеры;
5. запускает PostgreSQL;
6. применяет EF Core migrations;
7. запускает API и frontend;
8. выводит состояние сервисов.

Проверить состояние вручную:

```bash
docker compose ps
```

Просмотреть логи:

```bash
docker compose logs -f
```

Логи конкретного сервиса:

```bash
docker compose logs -f frontend
docker compose logs -f backend
docker compose logs -f postgres
```

## 5. Открытие приложения

По умолчанию доступны:

- frontend: [https://localhost:4200](https://localhost:4200);
- Swagger UI: [http://localhost:5081/swagger](http://localhost:5081/swagger);
- backend: `http://127.0.0.1:5081`;
- PostgreSQL: `localhost:5432`.

Frontend обслуживает browser-facing `/api/v1/*` через Nginx. API key добавляется proxy и не попадает в Angular bundle.

## 6. Первый вход

Используйте:

- табельный номер из `BOOTSTRAP_ADMIN_PERSONAL_ID`, по умолчанию `КП-0001`;
- пароль из `BOOTSTRAP_ADMIN_PASSWORD`.

При первом успешном запуске bootstrap-пароль шифруется для администратора. После входа администратор выбирает режим:

- **Моя страница**;
- **Администрирование приложения**.

Подробное описание интерфейса приведено в [руководстве пользователя](UserGuide.md).

## 7. Запуск frontend и backend отдельно

### Backend

Настройте строку подключения и секреты через .NET User Secrets, как описано в [Backend README](../backend/README.md), затем выполните:

```bash
dotnet tool restore
dotnet tool run dotnet-ef database update \
  --project backend/Certifications.Infrastructure/Certifications.Infrastructure.csproj \
  --startup-project backend/Certifications.Api/Certifications.Api.csproj \
  --context CertificationsDbContext

dotnet run --project backend/Certifications.Api --launch-profile https
```

### Frontend

В другом терминале:

```bash
cd frontend/certifications-ui
npm ci
npm run api:generate
npm start
```

Frontend dev proxy прочитает `SECURITY_API_KEY` из корневого `.env` и направит запросы на `https://localhost:7055`.

## 8. Проверка проекта

Backend:

```bash
dotnet build Certifications.slnx
dotnet test Certifications.slnx
```

Frontend:

```bash
cd frontend/certifications-ui
npm test -- --watch=false
npm run build
```

## 9. Остановка и очистка

Остановить контейнеры, сохранив базу:

```bash
docker compose down
```

Удалить контейнеры и локальные volumes:

```bash
docker compose down --volumes
```

Команда с `--volumes` безвозвратно удаляет локальную PostgreSQL database и Data Protection keys.

## 10. Типовые проблемы

### Браузер не доверяет сертификату

Повторите:

```bash
dotnet dev-certs https --clean
dotnet dev-certs https --trust
```

Затем перезапустите браузер и приложение.

### `npm start` сообщает об отсутствии `.env`

Создайте корневой `.env` из `.env.example` и заполните `SECURITY_API_KEY`.

### Login возвращает `401`

Проверьте:

- bootstrap Personal ID и пароль;
- наличие активного контракта у пользователя;
- совпадение API key в proxy и backend;
- работу secure cookie через HTTPS.

### Backend не подключается к PostgreSQL

Проверьте `POSTGRES_DB`, `POSTGRES_USER`, `POSTGRES_PASSWORD` и порт. Если пароль был изменён после создания volume, PostgreSQL не меняет сохранённый пароль автоматически. Для пустой локальной базы можно удалить volume и создать его заново, но все локальные данные будут потеряны.

### Порт уже занят

Измените `FRONTEND_PORT`, `BACKEND_PORT` или `POSTGRES_PORT` в `.env`. При изменении frontend URL также обновите `FRONTEND_ORIGIN`.
