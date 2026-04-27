namespace FridgeChef.SharedKernel;

public static class DomainErrors
{
    public static class Auth
    {
        public static readonly DomainError EmailAlreadyExists =
            new("AUTH_EMAIL_TAKEN", "Пользователь с таким email уже зарегистрирован");
        public static readonly DomainError InvalidCredentials =
            new("AUTH_INVALID_CREDENTIALS", "Неверный email или пароль");
        public static readonly DomainError InvalidRefreshToken =
            new("AUTH_INVALID_REFRESH_TOKEN", "Refresh token невалидный или истёк");
        public static readonly DomainError WrongPassword =
            new("AUTH_WRONG_PASSWORD", "Текущий пароль указан неверно");
        public static readonly DomainError AccountBlocked =
            new("AUTH_ACCOUNT_BLOCKED", "Аккаунт заблокирован администратором");
    }

    public static class NotFound
    {
        public static DomainError Recipe(Guid id) =>
            new("NOT_FOUND_RECIPE", $"Рецепт {id} не найден");
        public static DomainError RecipeBySlug(string slug) =>
            new("NOT_FOUND_RECIPE", $"Рецепт '{slug}' не найден");
        public static DomainError User(Guid id) =>
            new("NOT_FOUND_USER", $"Пользователь {id} не найден");
        public static DomainError PantryItem(Guid id) =>
            new("NOT_FOUND_PANTRY_ITEM", $"Продукт в холодильнике {id} не найден");
        public static DomainError FoodNode(long id) =>
            new("NOT_FOUND_FOOD_NODE", $"Продукт {id} не найден");
        public static DomainError Taxon(long id) =>
            new("NOT_FOUND_TAXON", $"Таксон {id} не найден");
    }

    public static class Pantry
    {
        public static readonly DomainError AlreadyExists =
            new("PANTRY_ALREADY_EXISTS", "Этот продукт уже добавлен в холодильник");
    }

    public static class Favorites
    {
        public static readonly DomainError AlreadyExists =
            new("FAVORITE_ALREADY_EXISTS", "Рецепт уже в избранном");
    }

    public static DomainError Validation(string field, string message) =>
        new($"VALIDATION_{field.ToUpperInvariant()}", message);
}
