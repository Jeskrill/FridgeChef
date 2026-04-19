namespace FridgeChef.Application.Recipes.Dto;

public sealed record RecipeCardResponse(
    Guid Id,
    string Slug,
    string Title,
    string? ImageUrl,
    int? TotalTimeMin,
    int? ActiveTimeMin,
    decimal? ServingsCount,
    decimal? KcalPerServing,
    int IngredientCount);

public sealed record RecipeDetailResponse(
    Guid Id,
    string Slug,
    string Title,
    string? ShortDescription,
    string? FullDescription,
    string SourceUrl,
    string? SourceAuthorName,
    int? TotalTimeMin,
    int? ActiveTimeMin,
    decimal? ServingsCount,
    NutritionResponse? Nutrition,
    IReadOnlyList<RecipeIngredientResponse> Ingredients,
    IReadOnlyList<RecipeStepResponse> Steps,
    IReadOnlyList<RecipeMediaResponse> Media,
    IReadOnlyList<string> Equipment,
    IReadOnlyList<long> AllergenNodeIds);

public sealed record RecipeIngredientResponse(
    long Id,
    string DisplayName,
    long? FoodNodeId,
    decimal? QuantityValue,
    string? QuantityTextRaw,
    string? IngredientNote,
    bool IsOptional,
    int Position);

public sealed record RecipeStepResponse(
    int StepNo,
    string? Title,
    string InstructionText,
    string? ImageUrl,
    int? DurationMin);

public sealed record RecipeMediaResponse(
    string Url,
    string Kind,
    int SortOrder);

public sealed record NutritionResponse(
    decimal? KcalPer100g,
    decimal? ProteinPer100g,
    decimal? FatPer100g,
    decimal? CarbsPer100g,
    decimal? KcalPerServing,
    decimal? ProteinPerServing,
    decimal? FatPerServing,
    decimal? CarbsPerServing);
