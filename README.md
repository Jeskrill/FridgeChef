# FridgeChef — Backend

Сервис подбора рецептов из продуктов в холодильнике пользователя. Бэкенд реализован на .NET 8 (Minimal API) с PostgreSQL

## Содержание

- [Стек технологий](#стек-технологий)
- [Архитектура](#архитектура)
- [Модули](#модули)
- [Вертикальный срез](#вертикальный-срез-на-примере-pantry)
- [Аутентификация и авторизация](#аутентификация-и-авторизация)
- [API](#api)
- [Правки](#правки)

----

## Стек технологий

- Runtime — .NET 8, ASP.NET Core Minimal API
- БД — PostgreSQL + EF Core 8 (Npgsql)
- Аутентификация — JWT Bearer (access + refresh tokens)
- Пароли — BCrypt (work factor 12)
- Валидация — FluentValidation
- Логирование — Serilog
- Тесты — xUnit v3 + FluentAssertions + NSubstitute

---

## Архитектура

Проект построен по принципу **модульного монолита** — каждый бизнес-контекст (Bounded Context) выделен в отдельную группу проектов с чёткими границами.

Внутри каждого модуля — **трёхслойная архитектура**:

```
┌──────────────────────────────────────────────────────┐
│  FridgeChef.Api  (Presentation — Minimal API)        │
│  Endpoints: маршрутизация, валидация, HTTP-ответы    │
├──────────────────────────────────────────────────────┤
│  *.Application  (Business Logic)                     │
│  Handlers, DTO, интерфейсы репозиториев              │
├──────────────────────────────────────────────────────┤
│  *.Infrastructure  (Data Access)                     │
│  EF Core DbContext, Entity-модели, маппинг в DTO     │
├──────────────────────────────────────────────────────┤
│  *.Domain  (при наличии)                             │
│  Доменные модели ядра, перечисления                  │
└──────────────────────────────────────────────────────┘
```

Ключевой принцип: **зависимости направлены внутрь** — Endpoint зависит от Application, Infrastructure реализует интерфейсы Application. Domain не зависит ни от чего.

---

## Модули

Каждый модуль (кроме Shared) содержит три проекта: Domain, Application, Infrastructure.

- **Auth** (Domain / Application / Infrastructure) — регистрация, вход, JWT-токены, профиль
- **Catalog** (Domain / Application / Infrastructure) — каталог рецептов, поиск, фильтрация, подбор по холодильнику
- **Ontology** (Domain / Application / Infrastructure) — база знаний: продукты (FoodNode), единицы измерения, таксоны
- **Pantry** (Domain / Application / Infrastructure) — холодильник пользователя
- **Favorites** (Domain / Application / Infrastructure) — избранные рецепты
- **UserPreferences** (Domain / Application / Infrastructure) — диеты, аллергены, исключённые продукты
- **Pricing** (Domain / Application / Infrastructure) — цены на продукты (Пятёрочка)
- **Admin** (Domain / Application / Infrastructure) — панель администратора
- **Shared** (SharedKernel) — `Result<T>`, `DomainErrors`, `LikeHelper`

Domain хранит доменные модели и перечисления, Application — use-case handlers и DTO, Infrastructure — EF Core и внешние интеграции.

Admin.Infrastructure содержит кросс-модульные адаптеры для Auth и Favorites. Адаптеры для Ontology и Catalog остаются в инфраструктуре соответствующих модулей, так как им нужен доступ к внутренним DbContext'ам.

---

## Вертикальный срез (на примере Pantry)

Рассмотрим, как запрос `POST /pantry` проходит через все слои.

### 1. Presentation (Endpoint)

**Файл:** `FridgeChef.Api/Endpoints/Pantry/PantryEndpoints.cs`

```csharp
group.MapPost("/", async (
    HttpContext http,
    AddPantryItemRequest request,
    IValidator<AddPantryItemRequest> validator,
    [FromServices] AddPantryItemHandler handler,
    CancellationToken ct) =>
{
    var validation = await validator.ValidateAsync(request, ct);
    if (!validation.IsValid)
        return Results.ValidationProblem(validation.ToDictionary());

    var result = await handler.HandleAsync(http.User.GetUserId(), request, ct);
    return result.ToHttpResult(StatusCodes.Status201Created);
});
```

Endpoint извлекает `userId` из JWT-claims, прогоняет запрос через FluentValidation и делегирует обработку в Handler.

### 2. Application (Handler + DTO)

**Файл:** `Pantry.Application/UseCases/PantryHandlers.cs`

DTO запроса и ответа:
```csharp
public sealed record AddPantryItemRequest(long FoodNodeId, decimal? Quantity, long? UnitId);

public sealed record PantryItemResponse(
    Guid Id, long FoodNodeId, decimal? Quantity,
    long? UnitId, string QuantityMode, DateTime CreatedAt);
```

Handler содержит бизнес-логику:
```csharp
public sealed class AddPantryItemHandler(IPantryRepository pantry)
{
    public async Task<Result<PantryItemResponse>> HandleAsync(
        Guid userId, AddPantryItemRequest req, CancellationToken ct)
    {
        var exists = await pantry.ExistsAsync(userId, req.FoodNodeId, ct);
        if (exists) return DomainErrors.Pantry.AlreadyExists;

        var response = await pantry.AddAsync(userId, req, ct);
        return response;
    }
}
```

Handler оперирует **только DTO Application-слоя** (`AddPantryItemRequest` / `PantryItemResponse`). Репозиторий определён здесь как интерфейс `IPantryRepository`, а его реализация — в Infrastructure.

### 3. Infrastructure (Repository + Entity)

**Файл:** `Pantry.Infrastructure/Persistence/PantryPersistence.cs`

Модель БД (Entity):
```csharp
internal sealed class PantryItemEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public long FoodNodeId { get; set; }
    public decimal? QuantityValue { get; set; }
    public long? UnitId { get; set; }
    public string QuantityMode { get; set; } = "unknown";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    // ...
}
```

Репозиторий конвертирует DTO в Entity при записи, и Entity в DTO при чтении:
```csharp
public async Task<PantryItemResponse> AddAsync(
    Guid userId, AddPantryItemRequest request, CancellationToken ct)
{
    var entity = new PantryItemEntity
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        FoodNodeId = request.FoodNodeId,
        QuantityValue = request.Quantity,
        UnitId = request.UnitId,
        QuantityMode = request.UnitId.HasValue ? "exact" : "unknown",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
    _db.PantryItems.Add(entity);
    await _db.SaveChangesAsync(ct);
    return ToDto(entity);
}

private static PantryItemResponse ToDto(PantryItemEntity e) => new(
    Id: e.Id,
    FoodNodeId: e.FoodNodeId,
    Quantity: e.QuantityValue,
    UnitId: e.UnitId,
    QuantityMode: e.QuantityMode ?? "unknown",
    CreatedAt: e.CreatedAt);
```

Конвертация между Entity и DTO происходит **в репозитории** — Handler никогда не видит `PantryItemEntity`. Это обеспечивает чистое разделение слоёв: Application-слой не знает ни о EF Core, ни о структуре таблиц.

### Итого: поток данных

```
POST /pantry  { foodNodeId: 42, quantity: 500, unitId: 1 }
       |
       v
   Endpoint  :  валидация (FluentValidation)
       |           извлечение userId из JWT
       v
   Handler   :  бизнес-проверки (дубликат?)
       |           работает с AddPantryItemRequest (DTO)
       v
  Repository :  создаёт PantryItemEntity (модель БД)
       |           сохраняет через EF Core
       |           конвертирует обратно в PantryItemResponse (DTO)
       v
   Endpoint  :  возвращает 201 + JSON
```

---

## Аутентификация и авторизация

### JWT

Для авторизации используются **JWT Bearer-токены**, передаваемые в заголовке `Authorization: Bearer <token>`.

При регистрации или входе сервер возвращает пару токенов:
- **Access Token** — содержит claims (userId, email, role, displayName), подписан HMAC-SHA256, время жизни настраивается через `Jwt:ExpiryHours`
- **Refresh Token** — случайная 64-байтная строка, хранится в БД в виде SHA-256 хеша

Пример ответа `POST /auth/sessions`:
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "tKQFDoK5mdas...",
  "expiresAt": "2026-05-27T07:36:58Z"
}
```

Клиент передаёт `accessToken` в заголовке при вызове защищённых эндпоинтов. При истечении — обменивает `refreshToken` на новую пару через `POST /auth/tokens`.

### Хранение паролей

Пароли хешируются через **BCrypt** с work factor 12 — подбор пароля к одному хешу занимает ~300 мс на современном CPU.

### Rate Limiting

Эндпоинты авторизации защищены rate limiter'ом: **20 запросов в минуту на IP**. Административные эндпоинты — **5 запросов в минуту на пользователя**.

### Роли

Две роли: `user` и `admin`. Административные эндпоинты (`/admin/*`) защищены политикой `AdminOnly`, проверяющей claim `role=admin` в JWT.

---

## API

Всего **53 эндпоинта**:

### Auth — Аутентификация

- `POST /auth/registration` — регистрация
- `POST /auth/sessions` — вход
- `POST /auth/tokens` — обновление токена
- `DELETE /auth/sessions` — выход (инвалидация refresh-токенов)

### Profile — Профиль пользователя

- `GET /users/me` — данные профиля
- `PATCH /users/me` — обновление профиля
- `PUT /users/me/password` — смена пароля

### Pantry — Холодильник

- `GET /pantry` — список продуктов
- `POST /pantry` — добавить продукт
- `PATCH /pantry/{id}` — обновить количество
- `DELETE /pantry/{id}` — удалить продукт

### Recipes — Рецепты

- `GET /recipes` — каталог с фильтрацией и пагинацией
- `GET /recipes/{slug}` — детальная информация
- `POST /recipes/matches` — подбор из холодильника

### Favorites — Избранное

- `GET /favorites` — список избранного
- `PUT /favorites/{recipeId}` — добавить в избранное
- `DELETE /favorites/{recipeId}` — удалить из избранного

### Reference — Справочники

- `GET /units` — единицы измерения
- `GET /taxons` — диеты, кухни, типы блюд

### FoodNodes — Продукты

- `GET /food-nodes?q=` — поиск продуктов
- `GET /food-nodes/{id}` — детали продукта

### Pricing — Цены

- `GET /pricing/ingredients` — цены на продукты

### Settings — Пользовательские настройки

- `GET/PUT /settings/diets` — диеты по умолчанию
- `GET/PUT /settings/cuisines` — предпочтения кухонь
- `GET/POST/DELETE /settings/allergens` — аллергены
- `GET/POST/DELETE /settings/excluded-foods` — исключённые продукты
- `GET/POST/DELETE /settings/favorite-foods` — любимые продукты

### Admin — Администрирование (18 эндпоинтов)

Управление пользователями, рецептами, ингредиентами, таксонами и ценами. Требует роли `admin`.

---

## Дополнительные технические решения

### Глобальная обработка ошибок

Все ошибки проходят через `GlobalExceptionHandler`, который:
- Конвертирует `DomainError` в соответствующий HTTP-статус (404, 401, 403, 409, 400)
- Ловит PostgreSQL-исключения (`UniqueViolation` = 409, `ForeignKeyViolation` = 400)
- В Development-режиме возвращает детали ошибки, в Production — обобщённое сообщение
- Все ответы — в формате `ProblemDetails` (RFC 9457)

### Result-паттерн

Вместо выбрасывания исключений для бизнес-ошибок используется `Result<T>`:
```csharp
if (exists) return DomainErrors.Pantry.AlreadyExists;
```

Это позволяет обрабатывать ошибки без try/catch и делает flow явным.

### Защита от LIKE-инъекций

Все поисковые запросы с `ILike` проходят через `LikeHelper.EscapeForLike()`, экранирующий спецсимволы `%`, `_`, `\`.

### Каждый модуль — свой DbContext

Каждый Bounded Context использует собственный `DbContext` (`AuthDbContext`, `CatalogDbContext`, `PantryDbContext`, ...), что обеспечивает изоляцию данных между модулями.

---

## Правки

Исправлены следующие проблемы:

**Использование HTTP-глаголов в эндпоинтах:**
- Восстановлены семантически корректные HTTP-методы в соответствии с REST-конвенциями (POST для создания, PATCH для частичного обновления, PUT для идемпотентных операций, DELETE для удаления)

**Конвертация моделей в правильном слое:**
- Маппинг между Entity (модель БД) и DTO (модель Application-слоя) перенесён в репозиторий (Infrastructure). Раньше конвертация частично выполнялась в Handler'ах — теперь Application-слой работает исключительно с DTO и не знает о структуре БД
