namespace FridgeChef.Catalog.Domain;

public enum RecipeStatus
{
    Draft = 0,
    Published = 1,
    Hidden = 2
}

public enum MediaKind
{
    Hero = 0,
    Gallery = 1,
    Step = 2,
    Video = 3
}

public sealed record Recipe(
    Guid Id,
    long SourceRecipeId,
    string Slug,
    string Title,
    string? ShortDescription,
    string? FullDescription,
    string SourceUrl,
    string? SourceAuthorName,
    decimal? ServingsCount,
    int? TotalTimeMin,
    int? ActiveTimeMin,
    RecipeStatus Status,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<RecipeIngredient> Ingredients,
    IReadOnlyList<RecipeStep> Steps,
    IReadOnlyList<RecipeSection> Sections,
    IReadOnlyList<RecipeMedia> Media,
    RecipeNutrition? Nutrition,
    IReadOnlyList<RecipeEquipment> Equipment,
    IReadOnlyList<RecipeTaxon> RecipeTaxons,
    IReadOnlyList<RecipeAllergen> Allergens);

public sealed record RecipeIngredient(
    long Id,
    Guid RecipeId,
    long? RecipeSectionId,
    int Position,
    long? FoodNodeId,
    string RawName,
    string DisplayName,
    long? SourceFoodId,
    string? SourceFoodName,
    string? SourceFoodSlug,
    string? IngredientNote,
    decimal? QuantityValue,
    decimal? QuantityValuePerServing,
    long? UnitId,
    string? QuantityTextRaw,
    decimal? KcalPer100g,
    decimal? ProteinPer100g,
    decimal? FatPer100g,
    decimal? CarbsPer100g,
    int? GlycemicIndex,
    decimal? NormalizedAmountG,
    decimal? NormalizedAmountMl,
    bool IsOptional,
    string NormalizationStatus,
    decimal? MatchConfidence);

public sealed record RecipeStep(
    long Id,
    Guid RecipeId,
    int StepNo,
    string? Title,
    string InstructionText,
    string? ImageUrl,
    int? DurationMin);

public sealed record RecipeSection(
    long Id,
    Guid RecipeId,
    string Name,
    int Position);

public sealed record RecipeMedia(
    long Id,
    Guid RecipeId,
    MediaKind MediaKind,
    string Url,
    string? Provider,
    int? StepNo,
    int SortOrder);

public sealed record RecipeNutrition(
    Guid RecipeId,
    decimal? TotalWeightG,
    decimal? ServingWeightG,
    decimal? KcalPer100g,
    decimal? KcalPerServing,
    decimal? ProteinPer100g,
    decimal? FatPer100g,
    decimal? CarbsPer100g,
    decimal? ProteinPerServing,
    decimal? FatPerServing,
    decimal? CarbsPerServing);

public sealed record RecipeEquipment(
    long Id,
    Guid RecipeId,
    string EquipmentName,
    int Position);

public sealed record RecipeTaxon(
    Guid RecipeId,
    long TaxonId,
    long? SourceTaxonId,
    decimal Confidence);

public sealed record RecipeAllergen(
    Guid RecipeId,
    long AllergenNodeId,
    int EvidenceIngredientCount);
