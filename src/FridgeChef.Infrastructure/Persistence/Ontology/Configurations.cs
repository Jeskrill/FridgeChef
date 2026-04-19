using FridgeChef.Domain.Ontology;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FridgeChef.Infrastructure.Persistence.Ontology;

internal sealed class FoodNodeConfiguration : IEntityTypeConfiguration<FoodNode>
{
    public void Configure(EntityTypeBuilder<FoodNode> builder)
    {
        builder.ToTable("food_nodes", "ontology");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).HasColumnName("id").UseIdentityAlwaysColumn();
        builder.Property(n => n.CanonicalName).HasColumnName("canonical_name").IsRequired();
        builder.Property(n => n.NormalizedName).HasColumnName("normalized_name").IsRequired();
        builder.Property(n => n.Slug).HasColumnName("slug").IsRequired();
        builder.Property(n => n.NodeKind).HasColumnName("node_kind")
            .HasConversion(
                v => v.ToString().ToLowerInvariant(),
                v => SafeParseNodeKind(v));
        builder.Property(n => n.Status).HasColumnName("status")
            .HasConversion(
                v => v.ToString().ToLowerInvariant(),
                v => SafeParseStatus(v));
        builder.Property(n => n.MergedIntoId).HasColumnName("merged_into_id");
        builder.Property(n => n.DefaultUnitId).HasColumnName("default_unit_id");
        builder.Property(n => n.CreatedAt).HasColumnName("created_at");
        builder.Property(n => n.UpdatedAt).HasColumnName("updated_at");
        builder.HasIndex(n => n.Slug).IsUnique();
        builder.HasIndex(n => n.CanonicalName).IsUnique();
    }

    private static FoodNodeKind SafeParseNodeKind(string v) =>
        Enum.TryParse<FoodNodeKind>(v.Replace("_", ""), true, out var k) ? k : FoodNodeKind.Ingredient;

    private static FoodNodeStatus SafeParseStatus(string v) =>
        Enum.TryParse<FoodNodeStatus>(v.Replace("_", ""), true, out var k) ? k : FoodNodeStatus.Active;
}

internal sealed class FoodAliasConfiguration : IEntityTypeConfiguration<FoodAlias>
{
    public void Configure(EntityTypeBuilder<FoodAlias> builder)
    {
        builder.ToTable("food_aliases", "ontology");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id").UseIdentityAlwaysColumn();
        builder.Property(a => a.FoodNodeId).HasColumnName("food_node_id");
        builder.Property(a => a.AliasText).HasColumnName("alias_text").IsRequired();
        builder.Property(a => a.AliasNormalized).HasColumnName("alias_normalized").IsRequired();
        builder.Property(a => a.AliasType).HasColumnName("alias_type")
            .HasConversion(
                v => v.ToString().ToLowerInvariant(),
                v => SafeParseAliasType(v));
        builder.Property(a => a.LanguageCode).HasColumnName("language_code").HasDefaultValue("ru");
        builder.Property(a => a.Priority).HasColumnName("priority").HasDefaultValue(100);
        builder.Property(a => a.IsPreferred).HasColumnName("is_preferred").HasDefaultValue(false);
        builder.Property(a => a.CreatedAt).HasColumnName("created_at");
        builder.Property(a => a.UpdatedAt).HasColumnName("updated_at");
        builder.HasOne(a => a.FoodNode).WithMany(n => n.Aliases).HasForeignKey(a => a.FoodNodeId);
    }

    private static AliasType SafeParseAliasType(string v) =>
        Enum.TryParse<AliasType>(v.Replace("_", ""), true, out var k) ? k : AliasType.Synonym;
}

internal sealed class FoodEdgeConfiguration : IEntityTypeConfiguration<FoodEdge>
{
    public void Configure(EntityTypeBuilder<FoodEdge> builder)
    {
        builder.ToTable("food_edges", "ontology");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").UseIdentityAlwaysColumn();
        builder.Property(e => e.FromNodeId).HasColumnName("from_node_id");
        builder.Property(e => e.ToNodeId).HasColumnName("to_node_id");
        builder.Property(e => e.EdgeType).HasColumnName("edge_type");
        builder.Property(e => e.Confidence).HasColumnName("confidence");
        builder.Property(e => e.SourceKind).HasColumnName("source_kind");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.HasIndex(e => new { e.FromNodeId, e.ToNodeId, e.EdgeType }).IsUnique();
    }
}

internal sealed class FoodEdgeClosureConfiguration : IEntityTypeConfiguration<FoodEdgeClosure>
{
    public void Configure(EntityTypeBuilder<FoodEdgeClosure> builder)
    {
        builder.ToTable("food_edge_closure", "ontology");
        builder.HasKey(c => new { c.AncestorNodeId, c.DescendantNodeId, c.SemanticType });
        builder.Property(c => c.AncestorNodeId).HasColumnName("ancestor_node_id");
        builder.Property(c => c.DescendantNodeId).HasColumnName("descendant_node_id");
        builder.Property(c => c.SemanticType).HasColumnName("semantic_type");
        builder.Property(c => c.Depth).HasColumnName("depth");
    }
}

internal sealed class UnitConfiguration : IEntityTypeConfiguration<Unit>
{
    public void Configure(EntityTypeBuilder<Unit> builder)
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

internal sealed class FoodNutrientProfileConfiguration : IEntityTypeConfiguration<FoodNutrientProfile>
{
    public void Configure(EntityTypeBuilder<FoodNutrientProfile> builder)
    {
        builder.ToTable("food_nutrient_profiles", "ontology");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").UseIdentityAlwaysColumn();
        builder.Property(p => p.FoodNodeId).HasColumnName("food_node_id");
        builder.Property(p => p.SourceName).HasColumnName("source_name");
        builder.Property(p => p.SourceRecordId).HasColumnName("source_record_id");
        builder.Property(p => p.KcalPer100g).HasColumnName("kcal_per_100g");
        builder.Property(p => p.ProteinPer100g).HasColumnName("protein_per_100g");
        builder.Property(p => p.FatPer100g).HasColumnName("fat_per_100g");
        builder.Property(p => p.CarbsPer100g).HasColumnName("carbs_per_100g");
        builder.HasOne(p => p.FoodNode).WithMany().HasForeignKey(p => p.FoodNodeId);
    }
}
