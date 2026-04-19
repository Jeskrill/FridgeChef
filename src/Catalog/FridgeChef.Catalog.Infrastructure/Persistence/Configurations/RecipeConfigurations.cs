using FridgeChef.Catalog.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FridgeChef.Catalog.Infrastructure.Persistence.Configurations;

internal sealed class RecipeConfiguration : IEntityTypeConfiguration<RecipeEntity>
{
    public void Configure(EntityTypeBuilder<RecipeEntity> builder)
    {
        builder.ToTable("recipes", "catalog");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id");
        builder.Property(r => r.SourceRecipeId).HasColumnName("source_recipe_id");
        builder.Property(r => r.Slug).HasColumnName("slug").IsRequired();
        builder.Property(r => r.Title).HasColumnName("title").IsRequired();
        builder.Property(r => r.ShortDescription).HasColumnName("short_description");
        builder.Property(r => r.FullDescription).HasColumnName("full_description");
        builder.Property(r => r.SourceUrl).HasColumnName("source_url").IsRequired();
        builder.Property(r => r.SourceAuthorName).HasColumnName("source_author_name");
        builder.Property(r => r.ServingsCount).HasColumnName("servings_count");
        builder.Property(r => r.TotalTimeMin).HasColumnName("total_time_min");
        builder.Property(r => r.ActiveTimeMin).HasColumnName("active_time_min");
        builder.Property(r => r.Status).HasColumnName("status");
        builder.Property(r => r.CreatedAt).HasColumnName("created_at");
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at");
        builder.HasIndex(r => r.Slug).IsUnique();
    }
}

internal sealed class RecipeIngredientConfiguration : IEntityTypeConfiguration<RecipeIngredientEntity>
{
    public void Configure(EntityTypeBuilder<RecipeIngredientEntity> builder)
    {
        builder.ToTable("recipe_ingredients", "catalog");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasColumnName("id").UseIdentityAlwaysColumn();
        builder.Property(i => i.RecipeId).HasColumnName("recipe_id");
        builder.Property(i => i.RecipeSectionId).HasColumnName("recipe_section_id");
        builder.Property(i => i.Position).HasColumnName("position");
        builder.Property(i => i.FoodNodeId).HasColumnName("food_node_id");
        builder.Property(i => i.RawName).HasColumnName("raw_name").IsRequired();
        builder.Property(i => i.DisplayName).HasColumnName("display_name").IsRequired();
        builder.Property(i => i.SourceFoodId).HasColumnName("source_food_id");
        builder.Property(i => i.SourceFoodName).HasColumnName("source_food_name");
        builder.Property(i => i.SourceFoodSlug).HasColumnName("source_food_slug");
        builder.Property(i => i.IngredientNote).HasColumnName("ingredient_note");
        builder.Property(i => i.QuantityValue).HasColumnName("quantity_value");
        builder.Property(i => i.QuantityValuePerServing).HasColumnName("quantity_value_per_serving");
        builder.Property(i => i.UnitId).HasColumnName("unit_id");
        builder.Property(i => i.QuantityTextRaw).HasColumnName("quantity_text_raw");
        builder.Property(i => i.KcalPer100g).HasColumnName("kcal_per_100g");
        builder.Property(i => i.ProteinPer100g).HasColumnName("protein_per_100g");
        builder.Property(i => i.FatPer100g).HasColumnName("fat_per_100g");
        builder.Property(i => i.CarbsPer100g).HasColumnName("carbs_per_100g");
        builder.Property(i => i.GlycemicIndex).HasColumnName("glycemic_index");
        builder.Property(i => i.NormalizedAmountG).HasColumnName("normalized_amount_g");
        builder.Property(i => i.NormalizedAmountMl).HasColumnName("normalized_amount_ml");
        builder.Property(i => i.IsOptional).HasColumnName("is_optional").HasDefaultValue(false);
        builder.Property(i => i.NormalizationStatus).HasColumnName("normalization_status");
        builder.Property(i => i.MatchConfidence).HasColumnName("match_confidence");
        builder.HasOne(i => i.Recipe).WithMany(r => r.Ingredients).HasForeignKey(i => i.RecipeId);
    }
}

internal sealed class RecipeStepConfiguration : IEntityTypeConfiguration<RecipeStepEntity>
{
    public void Configure(EntityTypeBuilder<RecipeStepEntity> builder)
    {
        builder.ToTable("recipe_steps", "catalog");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id").UseIdentityAlwaysColumn();
        builder.Property(s => s.RecipeId).HasColumnName("recipe_id");
        builder.Property(s => s.StepNo).HasColumnName("step_no");
        builder.Property(s => s.Title).HasColumnName("title");
        builder.Property(s => s.InstructionText).HasColumnName("instruction_text").IsRequired();
        builder.Property(s => s.ImageUrl).HasColumnName("image_url");
        builder.Property(s => s.DurationMin).HasColumnName("duration_min");
        builder.HasOne(s => s.Recipe).WithMany(r => r.Steps).HasForeignKey(s => s.RecipeId);
    }
}

internal sealed class RecipeSectionConfiguration : IEntityTypeConfiguration<RecipeSectionEntity>
{
    public void Configure(EntityTypeBuilder<RecipeSectionEntity> builder)
    {
        builder.ToTable("recipe_sections", "catalog");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id").UseIdentityAlwaysColumn();
        builder.Property(s => s.RecipeId).HasColumnName("recipe_id");
        builder.Property(s => s.Name).HasColumnName("name").IsRequired();
        builder.Property(s => s.Position).HasColumnName("position");
        builder.HasOne(s => s.Recipe).WithMany(r => r.Sections).HasForeignKey(s => s.RecipeId);
    }
}

internal sealed class RecipeMediaConfiguration : IEntityTypeConfiguration<RecipeMediaEntity>
{
    public void Configure(EntityTypeBuilder<RecipeMediaEntity> builder)
    {
        builder.ToTable("recipe_media", "catalog");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id").UseIdentityAlwaysColumn();
        builder.Property(m => m.RecipeId).HasColumnName("recipe_id");
        builder.Property(m => m.MediaKind).HasColumnName("media_kind");
        builder.Property(m => m.Url).HasColumnName("url").IsRequired();
        builder.Property(m => m.Provider).HasColumnName("provider");
        builder.Property(m => m.StepNo).HasColumnName("step_no");
        builder.Property(m => m.SortOrder).HasColumnName("sort_order");
        builder.HasOne(m => m.Recipe).WithMany(r => r.Media).HasForeignKey(m => m.RecipeId);
    }
}

internal sealed class RecipeNutritionConfiguration : IEntityTypeConfiguration<RecipeNutritionEntity>
{
    public void Configure(EntityTypeBuilder<RecipeNutritionEntity> builder)
    {
        builder.ToTable("recipe_nutrition", "catalog");
        builder.HasKey(n => n.RecipeId);
        builder.Property(n => n.RecipeId).HasColumnName("recipe_id");
        builder.Property(n => n.TotalWeightG).HasColumnName("total_weight_g");
        builder.Property(n => n.ServingWeightG).HasColumnName("serving_weight_g");
        builder.Property(n => n.KcalPer100g).HasColumnName("kcal_per_100g");
        builder.Property(n => n.KcalPerServing).HasColumnName("kcal_per_serving");
        builder.Property(n => n.ProteinPer100g).HasColumnName("protein_per_100g");
        builder.Property(n => n.FatPer100g).HasColumnName("fat_per_100g");
        builder.Property(n => n.CarbsPer100g).HasColumnName("carbs_per_100g");
        builder.Property(n => n.ProteinPerServing).HasColumnName("protein_per_serving");
        builder.Property(n => n.FatPerServing).HasColumnName("fat_per_serving");
        builder.Property(n => n.CarbsPerServing).HasColumnName("carbs_per_serving");
        builder.HasOne(n => n.Recipe).WithOne(r => r.Nutrition)
            .HasForeignKey<RecipeNutritionEntity>(n => n.RecipeId);
    }
}

internal sealed class RecipeEquipmentConfiguration : IEntityTypeConfiguration<RecipeEquipmentEntity>
{
    public void Configure(EntityTypeBuilder<RecipeEquipmentEntity> builder)
    {
        builder.ToTable("recipe_equipment", "catalog");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").UseIdentityAlwaysColumn();
        builder.Property(e => e.RecipeId).HasColumnName("recipe_id");
        builder.Property(e => e.EquipmentName).HasColumnName("equipment_name").IsRequired();
        builder.Property(e => e.Position).HasColumnName("position");
        builder.HasOne(e => e.Recipe).WithMany(r => r.Equipment).HasForeignKey(e => e.RecipeId);
    }
}

internal sealed class RecipeTaxonConfiguration : IEntityTypeConfiguration<RecipeTaxonEntity>
{
    public void Configure(EntityTypeBuilder<RecipeTaxonEntity> builder)
    {
        builder.ToTable("recipe_taxons", "catalog");
        builder.HasKey(rt => new { rt.RecipeId, rt.TaxonId });
        builder.Property(rt => rt.RecipeId).HasColumnName("recipe_id");
        builder.Property(rt => rt.TaxonId).HasColumnName("taxon_id");
        builder.Property(rt => rt.SourceTaxonId).HasColumnName("source_taxon_id");
        builder.Property(rt => rt.Confidence).HasColumnName("confidence");
        builder.HasOne(rt => rt.Recipe).WithMany(r => r.RecipeTaxons).HasForeignKey(rt => rt.RecipeId);
    }
}

internal sealed class RecipeAllergenConfiguration : IEntityTypeConfiguration<RecipeAllergenEntity>
{
    public void Configure(EntityTypeBuilder<RecipeAllergenEntity> builder)
    {
        builder.ToTable("recipe_allergens", "catalog");
        builder.HasKey(ra => new { ra.RecipeId, ra.AllergenNodeId });
        builder.Property(ra => ra.RecipeId).HasColumnName("recipe_id");
        builder.Property(ra => ra.AllergenNodeId).HasColumnName("allergen_node_id");
        builder.Property(ra => ra.EvidenceIngredientCount).HasColumnName("evidence_ingredient_count");
        builder.HasOne(ra => ra.Recipe).WithMany(r => r.Allergens).HasForeignKey(ra => ra.RecipeId);
    }
}
