using FridgeChef.Catalog.Application.Dto;
using FridgeChef.Catalog.Domain;

namespace FridgeChef.Catalog.Application.Converters;

/// <summary>
/// Converts Domain records to API-facing DTOs.
/// Domain model → Response DTO. No DB knowledge here.
/// </summary>
public static class RecipeConverter
{
    public static RecipeCardResponse ToCardDto(this Recipe recipe) => new(
        Id: recipe.Id,
        Slug: recipe.Slug,
        Title: recipe.Title,
        ImageUrl: recipe.Media.FirstOrDefault(m => m.MediaKind == MediaKind.Hero)?.Url,
        TotalTimeMin: recipe.TotalTimeMin,
        ActiveTimeMin: recipe.ActiveTimeMin,
        ServingsCount: recipe.ServingsCount,
        KcalPerServing: recipe.Nutrition?.KcalPerServing,
        IngredientCount: recipe.Ingredients.Count);

    public static RecipeDetailResponse ToDetailDto(this Recipe recipe) => new(
        Id: recipe.Id,
        Slug: recipe.Slug,
        Title: recipe.Title,
        ShortDescription: recipe.ShortDescription,
        FullDescription: recipe.FullDescription,
        SourceUrl: recipe.SourceUrl,
        SourceAuthorName: recipe.SourceAuthorName,
        TotalTimeMin: recipe.TotalTimeMin,
        ActiveTimeMin: recipe.ActiveTimeMin,
        ServingsCount: recipe.ServingsCount,
        Nutrition: recipe.Nutrition?.ToDto(),
        Ingredients: recipe.Ingredients
            .Select(i => i.ToDto())
            .ToList(),
        Steps: recipe.Steps
            .Select(s => s.ToDto())
            .ToList(),
        Media: recipe.Media
            .Select(m => m.ToDto())
            .ToList(),
        Equipment: recipe.Equipment
            .Select(e => e.EquipmentName)
            .ToList(),
        AllergenNodeIds: recipe.Allergens
            .Select(a => a.AllergenNodeId)
            .ToList());

    private static RecipeIngredientResponse ToDto(this RecipeIngredient ingredient) => new(
        Id: ingredient.Id,
        DisplayName: ingredient.DisplayName,
        FoodNodeId: ingredient.FoodNodeId,
        QuantityValue: ingredient.QuantityValue,
        QuantityTextRaw: ingredient.QuantityTextRaw,
        IngredientNote: ingredient.IngredientNote,
        IsOptional: ingredient.IsOptional,
        Position: ingredient.Position);

    private static RecipeStepResponse ToDto(this RecipeStep step) => new(
        StepNo: step.StepNo,
        Title: step.Title,
        InstructionText: step.InstructionText,
        ImageUrl: step.ImageUrl,
        DurationMin: step.DurationMin);

    private static RecipeMediaResponse ToDto(this RecipeMedia media) => new(
        Url: media.Url,
        Kind: media.MediaKind.ToString(),
        SortOrder: media.SortOrder);

    private static NutritionResponse ToDto(this RecipeNutrition nutrition) => new(
        KcalPer100g: nutrition.KcalPer100g,
        ProteinPer100g: nutrition.ProteinPer100g,
        FatPer100g: nutrition.FatPer100g,
        CarbsPer100g: nutrition.CarbsPer100g,
        KcalPerServing: nutrition.KcalPerServing,
        ProteinPerServing: nutrition.ProteinPerServing,
        FatPerServing: nutrition.FatPerServing,
        CarbsPerServing: nutrition.CarbsPerServing);
}
