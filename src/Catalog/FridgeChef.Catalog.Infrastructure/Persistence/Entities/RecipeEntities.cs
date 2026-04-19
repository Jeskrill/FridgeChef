namespace FridgeChef.Catalog.Infrastructure.Persistence.Entities;

// ────────────────────────────────────────────────────────────────────
//  Internal EF Core entities — never exposed outside Infrastructure.
//  These classes exist purely to interface with the database.
//  All public-facing types are Domain records.
// ────────────────────────────────────────────────────────────────────

internal sealed class RecipeEntity
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
    public string Status { get; set; } = "published";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<RecipeIngredientEntity> Ingredients { get; set; } = [];
    public ICollection<RecipeStepEntity> Steps { get; set; } = [];
    public ICollection<RecipeSectionEntity> Sections { get; set; } = [];
    public ICollection<RecipeMediaEntity> Media { get; set; } = [];
    public RecipeNutritionEntity? Nutrition { get; set; }
    public ICollection<RecipeEquipmentEntity> Equipment { get; set; } = [];
    public ICollection<RecipeTaxonEntity> RecipeTaxons { get; set; } = [];
    public ICollection<RecipeAllergenEntity> Allergens { get; set; } = [];
}

internal sealed class RecipeIngredientEntity
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

    public RecipeEntity Recipe { get; set; } = null!;
}

internal sealed class RecipeStepEntity
{
    public long Id { get; set; }
    public Guid RecipeId { get; set; }
    public int StepNo { get; set; }
    public string? Title { get; set; }
    public string InstructionText { get; set; } = null!;
    public string? ImageUrl { get; set; }
    public int? DurationMin { get; set; }

    public RecipeEntity Recipe { get; set; } = null!;
}

internal sealed class RecipeSectionEntity
{
    public long Id { get; set; }
    public Guid RecipeId { get; set; }
    public string Name { get; set; } = null!;
    public int Position { get; set; }

    public RecipeEntity Recipe { get; set; } = null!;
}

internal sealed class RecipeMediaEntity
{
    public long Id { get; set; }
    public Guid RecipeId { get; set; }
    public string MediaKind { get; set; } = null!;
    public string Url { get; set; } = null!;
    public string? Provider { get; set; }
    public int? StepNo { get; set; }
    public int SortOrder { get; set; }

    public RecipeEntity Recipe { get; set; } = null!;
}

internal sealed class RecipeNutritionEntity
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

    public RecipeEntity Recipe { get; set; } = null!;
}

internal sealed class RecipeEquipmentEntity
{
    public long Id { get; set; }
    public Guid RecipeId { get; set; }
    public string EquipmentName { get; set; } = null!;
    public int Position { get; set; }

    public RecipeEntity Recipe { get; set; } = null!;
}

internal sealed class RecipeTaxonEntity
{
    public Guid RecipeId { get; set; }
    public long TaxonId { get; set; }
    public long? SourceTaxonId { get; set; }
    public decimal Confidence { get; set; } = 1.0m;

    public RecipeEntity Recipe { get; set; } = null!;
}

internal sealed class RecipeAllergenEntity
{
    public Guid RecipeId { get; set; }
    public long AllergenNodeId { get; set; }
    public int EvidenceIngredientCount { get; set; }

    public RecipeEntity Recipe { get; set; } = null!;
}
