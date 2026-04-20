using FridgeChef.Domain.Auth;
using FridgeChef.Domain.Catalog;
using FridgeChef.Domain.Pantry;
using FridgeChef.Domain.UserPreferences;
using FridgeChef.Domain.Favorites;
using FridgeChef.Domain.Ontology;
using FridgeChef.Domain.Taxonomy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FridgeChef.Infrastructure.Persistence.UserDomain;

internal sealed class PantryItemConfiguration : IEntityTypeConfiguration<PantryItem>
{
    public void Configure(EntityTypeBuilder<PantryItem> builder)
    {
        builder.ToTable("pantry_items", "user_domain");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.UserId).HasColumnName("user_id");
        builder.Property(p => p.FoodNodeId).HasColumnName("food_node_id");
        builder.Property(p => p.QuantityValue).HasColumnName("quantity_value");
        builder.Property(p => p.UnitId).HasColumnName("unit_id");
        builder.Property(p => p.QuantityMode).HasColumnName("quantity_mode")
            .HasConversion(v => v.ToString().ToLowerInvariant(), v => Enum.Parse<QuantityMode>(v, true));
        builder.Property(p => p.NormalizedAmountG).HasColumnName("normalized_amount_g");
        builder.Property(p => p.NormalizedAmountMl).HasColumnName("normalized_amount_ml");
        builder.Property(p => p.Source).HasColumnName("source");
        builder.Property(p => p.Note).HasColumnName("note");
        builder.Property(p => p.ExpiresAt).HasColumnName("expires_at");
        builder.Property(p => p.CreatedAt).HasColumnName("created_at");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at");
        builder.HasIndex(p => p.UserId);
        builder.HasIndex(p => new { p.UserId, p.FoodNodeId })
            .HasDatabaseName("uq_pantry_items_user_food_node")
            .IsUnique();
        builder.HasOne<User>().WithMany().HasForeignKey(p => p.UserId);
        builder.HasOne<FoodNode>().WithMany().HasForeignKey(p => p.FoodNodeId);
        builder.HasOne<Unit>().WithMany().HasForeignKey(p => p.UnitId);
    }
}

internal sealed class UserAllergenConfiguration : IEntityTypeConfiguration<UserAllergen>
{
    public void Configure(EntityTypeBuilder<UserAllergen> builder)
    {
        builder.ToTable("user_allergens", "user_domain");
        builder.HasKey(a => new { a.UserId, a.FoodNodeId });
        builder.Property(a => a.UserId).HasColumnName("user_id");
        builder.Property(a => a.FoodNodeId).HasColumnName("food_node_id");
        builder.Property(a => a.Severity).HasColumnName("severity")
            .HasConversion(v => v.ToString().ToLowerInvariant(), v => Enum.Parse<AllergenSeverity>(v, true));
        builder.Property(a => a.CreatedAt).HasColumnName("created_at");
        builder.HasOne<User>().WithMany().HasForeignKey(a => a.UserId);
        builder.HasOne<FoodNode>().WithMany().HasForeignKey(a => a.FoodNodeId);
    }
}

internal sealed class UserExcludedFoodConfiguration : IEntityTypeConfiguration<UserExcludedFood>
{
    public void Configure(EntityTypeBuilder<UserExcludedFood> builder)
    {
        builder.ToTable("user_excluded_foods", "user_domain");
        builder.HasKey(e => new { e.UserId, e.FoodNodeId });
        builder.Property(e => e.UserId).HasColumnName("user_id");
        builder.Property(e => e.FoodNodeId).HasColumnName("food_node_id");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.HasOne<User>().WithMany().HasForeignKey(e => e.UserId);
        builder.HasOne<FoodNode>().WithMany().HasForeignKey(e => e.FoodNodeId);
    }
}

internal sealed class UserFavoriteFoodConfiguration : IEntityTypeConfiguration<UserFavoriteFood>
{
    public void Configure(EntityTypeBuilder<UserFavoriteFood> builder)
    {
        builder.ToTable("user_favorite_foods", "user_domain");
        builder.HasKey(f => new { f.UserId, f.FoodNodeId });
        builder.Property(f => f.UserId).HasColumnName("user_id");
        builder.Property(f => f.FoodNodeId).HasColumnName("food_node_id");
        builder.Property(f => f.Weight).HasColumnName("weight");
        builder.Property(f => f.CreatedAt).HasColumnName("created_at");
        builder.HasOne<User>().WithMany().HasForeignKey(f => f.UserId);
        builder.HasOne<FoodNode>().WithMany().HasForeignKey(f => f.FoodNodeId);
    }
}

internal sealed class UserDefaultDietConfiguration : IEntityTypeConfiguration<UserDefaultDiet>
{
    public void Configure(EntityTypeBuilder<UserDefaultDiet> builder)
    {
        builder.ToTable("user_default_diets", "user_domain");
        builder.HasKey(d => new { d.UserId, d.TaxonId });
        builder.Property(d => d.UserId).HasColumnName("user_id");
        builder.Property(d => d.TaxonId).HasColumnName("taxon_id");
        builder.Property(d => d.CreatedAt).HasColumnName("created_at");
        builder.HasOne<User>().WithMany().HasForeignKey(d => d.UserId);
        builder.HasOne<Taxon>().WithMany().HasForeignKey(d => d.TaxonId);
    }
}

internal sealed class FavoriteRecipeConfiguration : IEntityTypeConfiguration<FavoriteRecipe>
{
    public void Configure(EntityTypeBuilder<FavoriteRecipe> builder)
    {
        builder.ToTable("favorite_recipes", "user_domain");
        builder.HasKey(f => new { f.UserId, f.RecipeId });
        builder.Property(f => f.UserId).HasColumnName("user_id");
        builder.Property(f => f.RecipeId).HasColumnName("recipe_id");
        builder.Property(f => f.CreatedAt).HasColumnName("created_at");
        builder.HasOne<User>().WithMany().HasForeignKey(f => f.UserId);
        builder.HasOne<Recipe>().WithMany().HasForeignKey(f => f.RecipeId);
    }
}
