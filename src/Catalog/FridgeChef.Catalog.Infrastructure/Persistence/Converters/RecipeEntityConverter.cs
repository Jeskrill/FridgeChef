using FridgeChef.Catalog.Domain;
using FridgeChef.Catalog.Infrastructure.Persistence.Entities;

namespace FridgeChef.Catalog.Infrastructure.Persistence.Converters;

// Converts internal EF Core entities to pure Domain records.
// This layer is the only place that knows about both Entity and Domain shapes.
internal static class RecipeEntityConverter
{
    internal static Recipe ToDomain(this RecipeEntity e) => new(
        Id: e.Id,
        SourceRecipeId: e.SourceRecipeId,
        Slug: e.Slug,
        Title: e.Title,
        ShortDescription: e.ShortDescription,
        FullDescription: e.FullDescription,
        SourceUrl: e.SourceUrl,
        SourceAuthorName: e.SourceAuthorName,
        ServingsCount: e.ServingsCount,
        TotalTimeMin: e.TotalTimeMin,
        ActiveTimeMin: e.ActiveTimeMin,
        Status: Enum.TryParse<RecipeStatus>(e.Status, ignoreCase: true, out var status)
            ? status
            : RecipeStatus.Published,
        CreatedAt: e.CreatedAt,
        UpdatedAt: e.UpdatedAt,
        Ingredients: e.Ingredients
            .OrderBy(i => i.Position)
            .Select(i => i.ToDomain())
            .ToList(),
        Steps: e.Steps
            .OrderBy(s => s.StepNo)
            .Select(s => s.ToDomain())
            .ToList(),
        Sections: e.Sections
            .OrderBy(s => s.Position)
            .Select(s => s.ToDomain())
            .ToList(),
        Media: e.Media
            .OrderBy(m => m.SortOrder)
            .Select(m => m.ToDomain())
            .ToList(),
        Nutrition: e.Nutrition?.ToDomain(),
        Equipment: e.Equipment
            .OrderBy(eq => eq.Position)
            .Select(eq => eq.ToDomain())
            .ToList(),
        RecipeTaxons: e.RecipeTaxons
            .Select(rt => rt.ToDomain())
            .ToList(),
        Allergens: e.Allergens
            .Select(a => a.ToDomain())
            .ToList());

    private static RecipeIngredient ToDomain(this RecipeIngredientEntity e) => new(
        Id: e.Id,
        RecipeId: e.RecipeId,
        RecipeSectionId: e.RecipeSectionId,
        Position: e.Position,
        FoodNodeId: e.FoodNodeId,
        RawName: e.RawName,
        DisplayName: e.DisplayName,
        SourceFoodId: e.SourceFoodId,
        SourceFoodName: e.SourceFoodName,
        SourceFoodSlug: e.SourceFoodSlug,
        IngredientNote: e.IngredientNote,
        QuantityValue: e.QuantityValue,
        QuantityValuePerServing: e.QuantityValuePerServing,
        UnitId: e.UnitId,
        QuantityTextRaw: e.QuantityTextRaw,
        KcalPer100g: e.KcalPer100g,
        ProteinPer100g: e.ProteinPer100g,
        FatPer100g: e.FatPer100g,
        CarbsPer100g: e.CarbsPer100g,
        GlycemicIndex: e.GlycemicIndex,
        NormalizedAmountG: e.NormalizedAmountG,
        NormalizedAmountMl: e.NormalizedAmountMl,
        IsOptional: e.IsOptional,
        NormalizationStatus: e.NormalizationStatus,
        MatchConfidence: e.MatchConfidence);

    private static RecipeStep ToDomain(this RecipeStepEntity e) => new(
        Id: e.Id,
        RecipeId: e.RecipeId,
        StepNo: e.StepNo,
        Title: e.Title,
        InstructionText: e.InstructionText,
        ImageUrl: e.ImageUrl,
        DurationMin: e.DurationMin);

    private static RecipeSection ToDomain(this RecipeSectionEntity e) => new(
        Id: e.Id,
        RecipeId: e.RecipeId,
        Name: e.Name,
        Position: e.Position);

    private static RecipeMedia ToDomain(this RecipeMediaEntity e) => new(
        Id: e.Id,
        RecipeId: e.RecipeId,
        MediaKind: Enum.TryParse<MediaKind>(e.MediaKind, ignoreCase: true, out var mk)
            ? mk
            : MediaKind.Hero,
        Url: e.Url,
        Provider: e.Provider,
        StepNo: e.StepNo,
        SortOrder: e.SortOrder);

    private static RecipeNutrition ToDomain(this RecipeNutritionEntity e) => new(
        RecipeId: e.RecipeId,
        TotalWeightG: e.TotalWeightG,
        ServingWeightG: e.ServingWeightG,
        KcalPer100g: e.KcalPer100g,
        KcalPerServing: e.KcalPerServing,
        ProteinPer100g: e.ProteinPer100g,
        FatPer100g: e.FatPer100g,
        CarbsPer100g: e.CarbsPer100g,
        ProteinPerServing: e.ProteinPerServing,
        FatPerServing: e.FatPerServing,
        CarbsPerServing: e.CarbsPerServing);

    private static RecipeEquipment ToDomain(this RecipeEquipmentEntity e) => new(
        Id: e.Id,
        RecipeId: e.RecipeId,
        EquipmentName: e.EquipmentName,
        Position: e.Position);

    private static RecipeTaxon ToDomain(this RecipeTaxonEntity e) => new(
        RecipeId: e.RecipeId,
        TaxonId: e.TaxonId,
        SourceTaxonId: e.SourceTaxonId,
        Confidence: e.Confidence);

    private static RecipeAllergen ToDomain(this RecipeAllergenEntity e) => new(
        RecipeId: e.RecipeId,
        AllergenNodeId: e.AllergenNodeId,
        EvidenceIngredientCount: e.EvidenceIngredientCount);
}
