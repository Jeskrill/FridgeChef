using FridgeChef.Ontology.Infrastructure.Persistence.Entities;
using FridgeChef.Taxonomy.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FridgeChef.Ontology.Infrastructure.Persistence.Configurations;

internal sealed class TaxonEntity
{
    public long Id { get; set; }
    public string Kind { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? Description { get; set; }
}

internal sealed class TaxonConfiguration : IEntityTypeConfiguration<TaxonEntity>
{
    public void Configure(EntityTypeBuilder<TaxonEntity> builder)
    {
        builder.ToTable("taxons", "taxonomy");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id").UseIdentityAlwaysColumn();
        builder.Property(t => t.Kind).HasColumnName("kind");
        builder.Property(t => t.Name).HasColumnName("name").IsRequired();
        builder.Property(t => t.Slug).HasColumnName("slug").IsRequired();
        builder.Property(t => t.Description).HasColumnName("description");
        builder.HasIndex(t => t.Slug).IsUnique();
    }
}
