using FridgeChef.Domain.Auth;
using FridgeChef.Domain.Catalog;
using FridgeChef.Domain.Favorites;
using FridgeChef.Domain.Ontology;
using FridgeChef.Domain.Pantry;
using FridgeChef.Domain.Pricing;
using FridgeChef.Domain.Taxonomy;
using FridgeChef.Domain.UserPreferences;
using Microsoft.EntityFrameworkCore;

namespace FridgeChef.Infrastructure.Persistence;

public sealed class FridgeChefDbContext : DbContext
{
    public FridgeChefDbContext(DbContextOptions<FridgeChefDbContext> options) : base(options)
    {
        // NoTrackingWithIdentityResolution: read-only by default, but split queries can
        // correctly resolve entity identity (needed for AsSplitQuery + Include chains)
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTrackingWithIdentityResolution;
    }

    // Auth
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    // Ontology
    public DbSet<FoodNode> FoodNodes => Set<FoodNode>();
    public DbSet<FoodAlias> FoodAliases => Set<FoodAlias>();
    public DbSet<FoodEdge> FoodEdges => Set<FoodEdge>();
    public DbSet<FoodEdgeClosure> FoodEdgeClosures => Set<FoodEdgeClosure>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<FoodNutrientProfile> FoodNutrientProfiles => Set<FoodNutrientProfile>();

    // Taxonomy
    public DbSet<Taxon> Taxons => Set<Taxon>();
    // Catalog
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<RecipeIngredient> RecipeIngredients => Set<RecipeIngredient>();
    public DbSet<RecipeStep> RecipeSteps => Set<RecipeStep>();
    public DbSet<RecipeSection> RecipeSections => Set<RecipeSection>();
    public DbSet<RecipeMedia> RecipeMedia => Set<RecipeMedia>();
    public DbSet<RecipeNutrition> RecipeNutritions => Set<RecipeNutrition>();
    public DbSet<RecipeEquipment> RecipeEquipment => Set<RecipeEquipment>();
    public DbSet<RecipeTaxon> RecipeTaxons => Set<RecipeTaxon>();
    public DbSet<RecipeAllergen> RecipeAllergens => Set<RecipeAllergen>();

    // Pantry
    public DbSet<PantryItem> PantryItems => Set<PantryItem>();

    // User Preferences
    public DbSet<UserAllergen> UserAllergens => Set<UserAllergen>();
    public DbSet<UserExcludedFood> UserExcludedFoods => Set<UserExcludedFood>();
    public DbSet<UserFavoriteFood> UserFavoriteFoods => Set<UserFavoriteFood>();
    public DbSet<UserDefaultDiet> UserDefaultDiets => Set<UserDefaultDiet>();

    // Favorites
    public DbSet<FavoriteRecipe> FavoriteRecipes => Set<FavoriteRecipe>();

    // Pricing
    public DbSet<Retailer> Retailers => Set<Retailer>();
    public DbSet<RetailerProduct> RetailerProducts => Set<RetailerProduct>();
    public DbSet<IngredientProductMatch> IngredientProductMatches => Set<IngredientProductMatch>();
    public DbSet<PriceSnapshot> PriceSnapshots => Set<PriceSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FridgeChefDbContext).Assembly);
    }
}
