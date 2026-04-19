using FridgeChef.Ontology.Infrastructure.Persistence.Configurations;
using FridgeChef.Ontology.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FridgeChef.Ontology.Infrastructure.Persistence;

internal sealed class OntologyDbContext : DbContext
{
    public OntologyDbContext(DbContextOptions<OntologyDbContext> options) : base(options)
    {
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTrackingWithIdentityResolution;
    }

    internal DbSet<FoodNodeEntity> FoodNodes => Set<FoodNodeEntity>();
    internal DbSet<FoodAliasEntity> FoodAliases => Set<FoodAliasEntity>();
    internal DbSet<FoodEdgeClosureEntity> FoodEdgeClosures => Set<FoodEdgeClosureEntity>();
    internal DbSet<UnitEntity> Units => Set<UnitEntity>();
    internal DbSet<TaxonEntity> Taxons => Set<TaxonEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OntologyDbContext).Assembly);
    }
}
