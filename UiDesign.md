# Проектирование пользовательского интерфейса

## 1. Назначение документа

Документ описывает рекомендуемую структуру frontend-приложения системы управления контрактами и сертификациями.

Утверждённый технологический стек:

- Angular, проект создаётся и сопровождается Angular CLI;
- Angular Material;
- TypeScript;
- Reactive Forms;
- REST API как единственный источник серверных данных;
- типизированный API client, сгенерированный из OpenAPI;
- пользовательская аутентификация через защищённую `HttpOnly` cookie;
- API key как дополнительная защита канала доступа к API.

Бизнес-требования определены в [Requirements.md](./Requirements.md), API-контракт и backend-соображения — в [ApiDesign.md](./ApiDesign.md).

## 2. Принципы frontend

- Angular не реализует окончательные бизнес-правила и права доступа; их контролирует API.
- UI выполняет раннюю клиентскую валидацию для удобства пользователя.
- Domain entities не копируются напрямую во frontend; используются API DTO.
- Все списки с потенциально большим объёмом данных используют серверную пагинацию, сортировку и фильтрацию.
- Статус и `EffectiveValidTo` приходят рассчитанными с backend.
- UI не получает и не хранит ключ шифрования.
- Расшифрованный пароль показывается только после явного действия пользователя и не сохраняется в browser storage.
- Frontend не имеет доступа к содержимому authentication cookie; браузер прикладывает `HttpOnly` cookie к разрешённым запросам автоматически.

## 3. Рекомендуемая структура Angular-проекта

```text
src/app
├── core
│   ├── auth
│   ├── guards
│   ├── interceptors
│   ├── api
│   └── error-handling
├── shared
│   ├── components
│   ├── pipes
│   ├── directives
│   └── material
├── features
│   ├── login
│   ├── mode-selection
│   ├── certifications
│   ├── employees
│   ├── employee-contract
│   ├── certification-form
│   └── my-page
├── layout
└── app.routes.ts
```

Рекомендуется использовать standalone components и lazy-loaded feature routes.

## 4. Маршруты

```text
/login
/select-mode

/admin/certifications
/admin/users
/admin/users/new
/admin/users/:employeeId
/admin/users/:employeeId/contract
/admin/certifications/:certificationId

/me
```

Поведение после входа:

```text
Обычный пользователь
→ /me

Администратор
→ /select-mode
→ /me или /admin/certifications
```

Последний выбор администратора приходит из `GET /auth/me` и используется как значение по умолчанию на экране выбора режима.

## 5. Guards и interceptors

### 5.1. Guards

```text
authGuard
adminGuard
activeContractGuard
anonymousOnlyGuard
```

- `authGuard` требует аутентифицированного пользователя.
- `adminGuard` требует `IsAdmin = true`.
- `activeContractGuard` проверяет серверное состояние текущего пользователя.
- Guards улучшают UX, но не заменяют серверную авторизацию.

### 5.2. Interceptors

```text
authInterceptor
apiErrorInterceptor
correlationIdInterceptor — опционально
```

`apiErrorInterceptor` централизованно обрабатывает:

- `401` — переход на login;
- `403` — сообщение об отсутствии прав;
- `409` — бизнес-конфликт;
- validation errors — передача ошибок соответствующей форме;
- network errors — общее уведомление.

HTTP client отправляет запросы с credentials, чтобы браузер прикладывал authentication cookie. Если API и UI находятся на разных origin, backend должен разрешать credentials только для конкретного frontend origin.

API key предпочтительно добавляется reverse proxy перед API. Если принято решение отправлять его непосредственно из Angular, interceptor добавляет заголовок `X-API-Key`, однако такое значение нельзя считать секретным: пользователь может извлечь его из frontend bundle или browser traffic.

## 6. Экраны

### 6.1. Login

Маршрут: `/login`

Компоненты Angular Material:

- `mat-card`;
- `mat-form-field`;
- `matInput`;
- `mat-button`;
- progress indicator во время запроса.

Поля:

- `PersonalId`;
- `Password`.

UI не сообщает, существует ли конкретный `PersonalId`; при ошибке показывается общее сообщение о неверных учётных данных или невозможности входа.

### 6.2. Выбор режима администратора

Маршрут: `/select-mode`

Варианты:

- **Моя страница**;
- **Администрирование приложения**.

Последний выбор выделяется по умолчанию. После выбора frontend вызывает `PUT /auth/preferred-mode` и переходит на соответствующий маршрут.

### 6.3. Таблица сертификаций

Маршрут: `/admin/certifications`

Рекомендуемые компоненты:

- `mat-table`;
- `mat-sort`;
- `mat-paginator`;
- `mat-form-field`;
- `mat-select`;
- date controls;
- custom `StatusBadgeComponent`;
- `mat-progress-bar` для загрузки.

Одна строка отображает:

- `PersonalId`;
- фамилию;
- имя и отчество;
- должность;
- подразделение;
- `ContractDate`;
- рассчитанное `EffectiveValidTo`;
- активность;
- данные последней сертификации;
- текущий статус.

Фильтры:

- имя/фамилия;
- подразделение;
- диапазон срока действия;
- статус;
- специальный фильтр неактивных сотрудников.

Сортировка:

- имя/фамилия;
- подразделение;
- срок действия;
- статус.

Фильтры, сортировка и пагинация отправляются в API. UI не загружает весь набор сотрудников для локальной обработки.

При отображении неактивных сотрудников используется статус `NotApplicable`.

### 6.4. Управление сотрудниками

Маршрут: `/admin/users`

Таблица пользователей должна поддерживать:

- создание сотрудника;
- открытие формы сотрудника;
- изменение данных;
- назначение/снятие `IsAdmin`;
- переход к активному контракту;
- закрытие активного контракта.

Физическое удаление пользователя отсутствует.

### 6.5. Создание сотрудника и первого контракта

Маршрут: `/admin/users/new`

Форма является одной транзакционной UI-операцией и содержит две логические секции.

**Сотрудник:**

- `PersonalId` — обязательно;
- `FirstName` — обязательно;
- `MiddleName` — опционально;
- `LastName` — обязательно;
- `IsAdmin`;
- сгенерированный пароль.

**Первый контракт:**

- `ContractDate` — обязательно;
- `Position` — обязательно;
- `Department` — опционально;
- `Division` — опционально;
- `ValidTo` — опционально;
- `WarningMonths = 3`;
- `AlertMonths = 1`;
- `ProlongationForYears = 1`.

Кнопка сохранения вызывает один API endpoint. Если API не создал контракт, UI не должен считать сотрудника созданным.

### 6.6. Редактирование сотрудника и контракта

Маршрут: `/admin/users/:employeeId`

Форма позволяет:

- изменить `PersonalId`;
- изменить персональные данные;
- назначить или снять `IsAdmin`;
- просмотреть текущий контракт;
- закрыть контракт;
- создать новый контракт после закрытия предыдущего;
- управлять сертификациями;
- сгенерировать и показать пароль.

При ответе `409 Conflict` из-за изменения данных другим администратором UI предлагает обновить данные с сервера.

### 6.7. Сертификации

На странице контракта показывается история сертификаций как таблица `ProlongationRow`.

Колонки:

- `Assessor`;
- `CertificationDate`;
- `ProtocolDate`;
- `ProlongationSend`;
- `ProlongationReturned`;
- состояние записи.

Кнопка **Добавить сертификацию** недоступна, если существует незавершённая сертификация.

Форма сертификации:

- при создании обязательны `CertificationDate` и `Assessor`;
- последующие даты становятся доступны последовательно;
- UI валидирует порядок дат;
- завершённая сертификация открывается только для чтения;
- операция заполнения `ProlongationReturned` требует подтверждения, поскольку необратимо завершает процесс и изменяет `ValidTo`.

### 6.8. Личная страница

Маршрут: `/me`

Страница показывает только данные текущего пользователя:

- персональные данные;
- активный контракт;
- `EffectiveValidTo`;
- статус;
- историю сертификаций;
- собственный пароль по явному действию.

Все данные read-only.

## 7. StatusBadgeComponent

Компонент получает значение `CertificationStatus` от API и отвечает только за представление.

Поддерживаемые значения:

```text
NotApplicable
ContractValid
CertificationPending
CertificationInProgress
CertificationMissing
```

Рекомендуемое визуальное различие:

| Статус | Семантика |
|---|---|
| `NotApplicable` | нейтральный |
| `ContractValid` | успешный |
| `CertificationPending` | предупреждение |
| `CertificationInProgress` | информационный/активный процесс |
| `CertificationMissing` | критический |

Цвет не должен быть единственным способом различения: необходимо отображать текст и при необходимости icon/tooltip.

## 8. Reactive Forms и валидация

Frontend повторяет основные проверки API:

- обязательность полей;
- минимальная длина пароля 8 символов;
- буквы и цифры в пароле;
- специальные символы разрешены;
- `AlertMonths < WarningMonths`;
- положительный `ProlongationForYears`;
- порядок дат сертификации.

Проверка уникальности `PersonalId` окончательно выполняется API. Клиент может показывать ошибку после ответа `409 Conflict`.

Ошибки API должны отображаться рядом с соответствующими полями, если сервер возвращает field-level validation details.

## 9. Работа с паролем

### 9.1. Генерация

- Генерация выполняется API.
- UI не генерирует пароль самостоятельно.
- Новый пароль показывается только после успешного ответа API.
- UI предупреждает, что предыдущий пароль немедленно перестанет работать.

### 9.2. Просмотр

- Пользователь может запросить только собственный пароль.
- Администратор может запросить пароль выбранного сотрудника.
- Перед раскрытием требуется явное действие, например кнопка **Показать пароль**.
- Значение по умолчанию скрыто.
- После закрытия диалога значение удаляется из component state.
- Пароль нельзя сохранять в `localStorage`, `sessionStorage`, URL или analytics events.
- Ответ нельзя помещать в application cache.

## 10. State management

На старте отдельная глобальная state-management библиотека не обязательна.

Достаточно:

- Angular services;
- signals/RxJS для локального и feature state;
- query parameters для состояния фильтров таблицы;
- generated API client для HTTP.

Глобально хранятся только:

- состояние текущего пользователя;
- факт аутентификации;
- роль;
- последний выбранный режим.

Пароль никогда не хранится в глобальном state.

## 11. OpenAPI client

Frontend не должен вручную дублировать API interfaces, если они могут быть сгенерированы из OpenAPI.

Рекомендуемый процесс:

```text
.NET Minimal API
→ /openapi/v1.json
→ TypeScript client generation
→ Angular API services/models
```

Generated files не редактируются вручную. Изменения начинаются с API DTO/OpenAPI, после чего клиент генерируется повторно.

## 12. Angular Material

Angular Material добавляется через Angular CLI:

```bash
ng add @angular/material
```

Версии `@angular/core`, `@angular/cli`, `@angular/material` и `@angular/cdk` должны быть совместимы и использовать один согласованный major release.

Основные Material-компоненты:

- form field/input/select/checkbox;
- date controls;
- table/sort/paginator;
- dialog;
- card;
- button/icon;
- progress indicator;
- snackbar;
- tooltip;
- chips или custom badge для статусов.

Официальная документация:

- [Angular CLI](https://angular.dev/tools/cli)
- [Angular Material](https://material.angular.dev/guide/getting-started)

## 13. Accessibility и responsive layout

- Все элементы управления имеют labels.
- Управление доступно с клавиатуры.
- Диалоги корректно управляют focus.
- Статусы различаются не только цветом.
- Ошибки форм связаны с соответствующими controls.
- Таблицы должны иметь адаптивное представление для узких экранов либо горизонтальный scroll с сохранением доступности.
- Основной целевой сценарий административных таблиц — desktop/tablet.

## 14. Тестирование

Unit/component tests:

- form validators;
- StatusBadgeComponent;
- route guards;
- mapping query parameters;
- обработка API validation errors.

Integration tests:

- login и маршрутизация по роли;
- выбор режима администратора;
- создание сотрудника вместе с контрактом;
- закрытие и создание нового контракта;
- создание и завершение сертификации;
- блокировка второй незавершённой сертификации;
- read-only личная страница;
- reveal/generate password flows;
- серверная фильтрация, сортировка и пагинация.

## 15. Незакрытые технические решения

До начала реализации необходимо утвердить или подтвердить:

1. Angular, Angular CLI, Angular Material и CDK major version 21, соответствующую установленному Angular CLI `21.2.3`.
2. Размещение UI и API: один origin или разные origin.
3. Инструмент генерации TypeScript client из OpenAPI.
4. Финальную тему Angular Material и правила responsive layout.
5. Добавляет ли `X-API-Key` reverse proxy или Angular interceptor; второй вариант не обеспечивает секретность ключа.
