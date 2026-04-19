using FridgeChef.Ontology.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FridgeChef.Ontology.Infrastructure.Persistence.Configurations;

internal sealed class FoodNodeConfiguration : IEntityTypeConfiguration<FoodNodeEntity>
{
    public void Configure(EntityTypeBuilder<FoodNodeEntity> builder)
    {
        builder.ToTable("food_nodes", "ontology");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).HasColumnName("id").UseIdentityAlwaysColumn();
        builder.Property(n => n.CanonicalName).HasColumnName("canonical_name").IsRequired();
        builder.Property(n => n.NormalizedName).HasColumnName("normalized_name").IsRequired();
        builder.Property(n => n.Slug).HasColumnName("slug").IsRequired();
        builder.Property(n => n.NodeKind).HasColumnName("node_kind");
        builder.Property(n => n.Status).HasColumnName("status");
        builder.Property(n => n.MergedIntoId).HasColumnName("merged_into_id");
        builder.Property(n => n.DefaultUnitId).HasColumnName("default_unit_id");
        builder.Property(n => n.CreatedAt).HasColumnName("created_at");
        builder.Property(n => n.UpdatedAt).HasColumnName("updated_at");
        builder.HasIndex(n => n.Slug).IsUnique();
        builder.HasIndex(n => n.CanonicalName).IsUnique();
    }
}

internal sealed class FoodAliasConfiguration : IEntityTypeConfiguration<FoodAliasEntity>
{
    public void Configure(EntityTypeBuilder<FoodAliasEntity> builder)
    {
        builder.ToTable("food_aliases", "ontology");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id").UseIdentityAlwaysColumn();
        builder.Property(a => a.FoodNodeId).HasColumnName("food_node_id");
        builder.Property(a => a.AliasText).HasColumnName("alias_text").IsRequired();
        builder.Property(a => a.AliasNormalized).HasColumnName("alias_normalized").IsRequired();
        builder.Property(a => a.AliasType).HasColumnName("alias_type");
        builder.Property(a => a.LanguageCode).HasColumnName("language_code").HasDefaultValue("ru");
        builder.Property(a => a.Priority).HasColumnName("priority").HasDefaultValue(100);
        builder.Property(a => a.IsPreferred).HasColumnName("is_preferred").HasDefaultValue(false);
        builder.Property(a => a.CreatedAt).HasColumnName("created_at");
        builder.Property(a => a.UpdatedAt).HasColumnName("updated_at");
        builder.HasOne(a => a.FoodNode).WithMany(n => n.Aliases).HasForeignKey(a => a.FoodNodeId);
    }
}

internal sealed class FoodEdgeClosureConfiguration : IEntityTypeConfiguration<FoodEdgeClosureEntity>
{
    public void Configure(EntityTypeBuilder<FoodEdgeClosureEntity> builder)
    {
        builder.ToTable("food_edge_closure", "ontology");
        builder.HasKey(c => new { c.AncestorNodeId, c.DescendantNodeId, c.SemanticType });
        builder.Property(c => c.AncestorNodeId).HasColumnName("ancestor_node_id");
        builder.Property(c => c.DescendantNodeId).HasColumnName("descendant_node_id");
        builder.Property(c => c.SemanticType).HasColumnName("semantic_type");
        builder.Property(c => c.Depth).HasColumnName("depth");
    }
}

internal sealed class UnitConfiguration : IEntityTypeConfiguration<UnitEntity>
{
    public void Configure(EntityTypeBuilder<UnitEntity> builder)
    {
        builder.ToTable("units", "ontology");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnName("id").UseIdentityAlwaysColumn();
        builder.Property(u => u.Code).HasColumnName("code").IsRequired();
        builder.Property(u => u.Name).HasColumnName("name").IsRequired();
        builder.Property(u => u.Symbol).HasColumnName("symbol").IsRequired();
        builder.Property(u => u.QuantityClass).HasColumnName("quantity_class").IsRequired();
        builder.Property(u => u.ToBaseMultiplier).HasColumnName("to_base_multiplier");
        builder.HasIndex(u => u.Code).IsUnique();
    }
}
