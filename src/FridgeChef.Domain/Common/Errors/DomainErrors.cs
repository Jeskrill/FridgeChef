namespace FridgeChef.Domain.Common;

/// <summary>
/// Represents a domain error with a machine-readable code and human-readable message.
/// </summary>
public sealed record DomainError(string Code, string Message)
{
    public override string ToString() => $"{Code}: {Message}";
}

/// <summary>
/// Catalog of well-known domain errors. Always add new errors here — never create them inline.
/// </summary>
public static class DomainErrors
{
    public static class Auth
    {
        public static readonly DomainError EmailAlreadyTaken     = new("AUTH_EMAIL_TAKEN",              "Пользователь с таким email уже зарегистрирован");
        public static readonly DomainError InvalidCredentials    = new("AUTH_INVALID_CREDENTIALS",      "Неверный email или пароль");
        public static readonly DomainError InvalidRefreshToken   = new("AUTH_INVALID_REFRESH_TOKEN",    "Refresh token невалидный или истёк");
        public static readonly DomainError WrongPassword         = new("AUTH_WRONG_PASSWORD",           "Текущий пароль указан неверно");
        public static readonly DomainError AccountBlocked        = new("AUTH_BLOCKED",                  "Аккаунт заблокирован");
    }

    public static class NotFound
    {
        public static DomainError Recipe(Guid id)       => new("NOT_FOUND_RECIPE",       $"Рецепт {id} не найден");
        public static DomainError RecipeBySlug(string slug) => new("NOT_FOUND_RECIPE",   $"Рецепт '{slug}' не найден");
        public static DomainError User(Guid id)         => new("NOT_FOUND_USER",         $"Пользователь {id} не найден");
        public static DomainError PantryItem(Guid id)   => new("NOT_FOUND_PANTRY_ITEM",  $"Продукт в холодильнике {id} не найден");
        public static DomainError FoodNode(long id)     => new("NOT_FOUND_FOOD_NODE",    $"Продукт {id} не найден");
    }

    public static class Pantry
    {
        public static readonly DomainError AlreadyExists = new("PANTRY_ALREADY_EXISTS", "Этот продукт уже добавлен в холодильник");
        public static readonly DomainError UnitRequiresQuantity =
            new("PANTRY_UNIT_REQUIRES_QUANTITY", "Нельзя указать единицу измерения без количества");
    }

    public static class Favorites
    {
        public static readonly DomainError AlreadyExists = new("FAVORITE_ALREADY_EXISTS", "Рецепт уже в избранном");
    }
}
