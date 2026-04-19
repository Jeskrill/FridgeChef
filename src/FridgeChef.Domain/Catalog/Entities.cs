namespace FridgeChef.Domain.Catalog;

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

/// <summary>Recipe entity mapped to catalog.recipes.</summary>
public sealed class Recipe
{
    public Guid Id { get; set; }
    public long SourceRecipeId { get; set; }
    public string Slug { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string? ShortDescription { get; set; }
    public string? FullDescription { get; set; }
    public string SourceUrl { get; set; } = null!;
    public string? SourceAuthorName { get; set; }
    public decimal? ServingsCount { get; set; }
    public int? TotalTimeMin { get; set; }
    public int? ActiveTimeMin { get; set; }
    public RecipeStatus Status { get; set; } = RecipeStatus.Published;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<RecipeIngredient> Ingredients { get; set; } = new List<RecipeIngredient>();
    public ICollection<RecipeStep> Steps { get; set; } = new List<RecipeStep>();
    public ICollection<RecipeSection> Sections { get; set; } = new List<RecipeSection>();
    public ICollection<RecipeMedia> Media { get; set; } = new List<RecipeMedia>();
    public RecipeNutrition? Nutrition { get; set; }
    public ICollection<RecipeEquipment> Equipment { get; set; } = new List<RecipeEquipment>();
    public ICollection<RecipeTaxon> RecipeTaxons { get; set; } = new List<RecipeTaxon>();
    public ICollection<RecipeAllergen> Allergens { get; set; } = new List<RecipeAllergen>();
}

/// <summary>Mapped to catalog.recipe_ingredients.</summary>
public sealed class RecipeIngredient
{
    public long Id { get; set; }
    public Guid RecipeId { get; set; }
    public long? RecipeSectionId { get; set; }
    public int Position { get; set; }
    public long? FoodNodeId { get; set; }
    public string RawName { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public long? SourceFoodId { get; set; }
    public string? SourceFoodName { get; set; }
    public string? SourceFoodSlug { get; set; }
    public string? IngredientNote { get; set; }
    public decimal? QuantityValue { get; set; }
    public decimal? QuantityValuePerServing { get; set; }
    public long? UnitId { get; set; }
    public string? QuantityTextRaw { get; set; }
    public decimal? KcalPer100g { get; set; }
    public decimal? ProteinPer100g { get; set; }
    public decimal? FatPer100g { get; set; }
    public decimal? CarbsPer100g { get; set; }
    public int? GlycemicIndex { get; set; }
    public decimal? NormalizedAmountG { get; set; }
    public decimal? NormalizedAmountMl { get; set; }
    public bool IsOptional { get; set; }
    public string NormalizationStatus { get; set; } = "matched";
    public decimal? MatchConfidence { get; set; }

    public Recipe Recipe { get; set; } = null!;
}

/// <summary>Mapped to catalog.recipe_steps.</summary>
public sealed class RecipeStep
{
    public long Id { get; set; }
    public Guid RecipeId { get; set; }
    public int StepNo { get; set; }
    public string? Title { get; set; }
    public string InstructionText { get; set; } = null!;
    public string? ImageUrl { get; set; }
    public int? DurationMin { get; set; }

    public Recipe Recipe { get; set; } = null!;
}

/// <summary>Mapped to catalog.recipe_sections.</summary>
public sealed class RecipeSection
{
    public long Id { get; set; }
    public Guid RecipeId { get; set; }
    public string Name { get; set; } = null!;
    public int Position { get; set; }

    public Recipe Recipe { get; set; } = null!;
}

/// <summary>Mapped to catalog.recipe_media.</summary>
public sealed class RecipeMedia
{
    public long Id { get; set; }
    public Guid RecipeId { get; set; }
    public MediaKind MediaKind { get; set; }
    public string Url { get; set; } = null!;
    public string? Provider { get; set; }
    public int? StepNo { get; set; }
    public int SortOrder { get; set; }

    public Recipe Recipe { get; set; } = null!;
}

/// <summary>Mapped to catalog.recipe_nutrition (1:1).</summary>
public sealed class RecipeNutrition
{
    public Guid RecipeId { get; set; }
    public decimal? TotalWeightG { get; set; }
    public decimal? ServingWeightG { get; set; }
    public decimal? KcalPer100g { get; set; }
    public decimal? KcalPerServing { get; set; }
    public decimal? ProteinPer100g { get; set; }
    public decimal? FatPer100g { get; set; }
    public decimal? CarbsPer100g { get; set; }
    public decimal? ProteinPerServing { get; set; }
    public decimal? FatPerServing { get; set; }
    public decimal? CarbsPerServing { get; set; }

    public Recipe Recipe { get; set; } = null!;
}

/// <summary>Mapped to catalog.recipe_equipment.</summary>
public sealed class RecipeEquipment
{
    public long Id { get; set; }
    public Guid RecipeId { get; set; }
    public string EquipmentName { get; set; } = null!;
    public int Position { get; set; }

    public Recipe Recipe { get; set; } = null!;
}

/// <summary>Mapped to catalog.recipe_taxons.</summary>
public sealed class RecipeTaxon
{
    public Guid RecipeId { get; set; }
    public long TaxonId { get; set; }
    public long? SourceTaxonId { get; set; }
    public decimal Confidence { get; set; } = 1.0m;

    public Recipe Recipe { get; set; } = null!;
}

/// <summary>Mapped to catalog.recipe_allergens.</summary>
public sealed class RecipeAllergen
{
    public Guid RecipeId { get; set; }
    public long AllergenNodeId { get; set; }
    public int EvidenceIngredientCount { get; set; }

    public Recipe Recipe { get; set; } = null!;
}
