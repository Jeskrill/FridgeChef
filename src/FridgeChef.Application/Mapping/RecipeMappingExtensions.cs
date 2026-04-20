using FridgeChef.Application.Recipes.Dto;
using FridgeChef.Domain.Catalog;

namespace FridgeChef.Application.Mapping;

public static class RecipeMappingExtensions
{
    public static RecipeCardResponse ToCardResponse(this Recipe recipe) =>
        new(
            Id: recipe.Id,
            Slug: recipe.Slug,
            Title: recipe.Title,
            ImageUrl: recipe.Media
                .Where(m => m.MediaKind == MediaKind.Hero)
                .OrderBy(m => m.SortOrder)
                .Select(m => m.Url)
                .FirstOrDefault(),
            TotalTimeMin: recipe.TotalTimeMin,
            ActiveTimeMin: recipe.ActiveTimeMin,
            ServingsCount: recipe.ServingsCount,
            KcalPerServing: recipe.Nutrition?.KcalPerServing,
            IngredientCount: recipe.Ingredients.Count);

    public static RecipeDetailResponse ToDetailResponse(this Recipe recipe) =>
        new(
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
            Nutrition: recipe.Nutrition?.ToResponse(),
            Ingredients: recipe.Ingredients
                .OrderBy(i => i.Position)
                .Select(i => i.ToResponse())
                .ToList(),
            Steps: recipe.Steps
                .OrderBy(s => s.StepNo)
                .Select(s => s.ToResponse())
                .ToList(),
            Media: recipe.Media
                .OrderBy(m => m.SortOrder)
                .Select(m => m.ToResponse())
                .ToList(),
            Equipment: recipe.Equipment
                .OrderBy(e => e.Position)
                .Select(e => e.EquipmentName)
                .ToList(),
            AllergenNodeIds: recipe.Allergens
                .Select(a => a.AllergenNodeId)
                .OrderBy(id => id)
                .ToList());

    private static RecipeIngredientResponse ToResponse(this RecipeIngredient ingredient) =>
        new(
            Id: ingredient.Id,
            DisplayName: ingredient.DisplayName,
            FoodNodeId: ingredient.FoodNodeId,
            QuantityValue: ingredient.QuantityValue,
            QuantityTextRaw: ingredient.QuantityTextRaw,
            IngredientNote: ingredient.IngredientNote,
            IsOptional: ingredient.IsOptional,
            Position: ingredient.Position);

    private static RecipeStepResponse ToResponse(this RecipeStep step) =>
        new(
            StepNo: step.StepNo,
            Title: step.Title,
            InstructionText: step.InstructionText,
            ImageUrl: step.ImageUrl,
            DurationMin: step.DurationMin);

    private static RecipeMediaResponse ToResponse(this RecipeMedia media) =>
        new(
            Url: media.Url,
            Kind: media.MediaKind.ToString(),
            SortOrder: media.SortOrder);

    private static NutritionResponse ToResponse(this RecipeNutrition nutrition) =>
        new(
            KcalPer100g: nutrition.KcalPer100g,
            ProteinPer100g: nutrition.ProteinPer100g,
            FatPer100g: nutrition.FatPer100g,
            CarbsPer100g: nutrition.CarbsPer100g,
            KcalPerServing: nutrition.KcalPerServing,
            ProteinPerServing: nutrition.ProteinPerServing,
            FatPerServing: nutrition.FatPerServing,
            CarbsPerServing: nutrition.CarbsPerServing);
}
