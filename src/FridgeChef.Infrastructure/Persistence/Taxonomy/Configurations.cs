using FridgeChef.Domain.Taxonomy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FridgeChef.Infrastructure.Persistence.Taxonomy;

internal sealed class TaxonConfiguration : IEntityTypeConfiguration<Taxon>
{
    public void Configure(EntityTypeBuilder<Taxon> builder)
    {
        builder.ToTable("taxons", "taxonomy");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id").UseIdentityAlwaysColumn();
        builder.Property(t => t.Kind).HasColumnName("kind")
            .HasConversion(
                v => v.ToString().ToLowerInvariant(),
                v => SafeParseTaxonKind(v));
        builder.Property(t => t.Name).HasColumnName("name").IsRequired();
        builder.Property(t => t.Slug).HasColumnName("slug").IsRequired();
        builder.Property(t => t.Description).HasColumnName("description");
        builder.HasIndex(t => new { t.Kind, t.Slug }).IsUnique();
    }

    private static TaxonKind SafeParseTaxonKind(string v) =>
        Enum.TryParse<TaxonKind>(v.Replace("_", ""), true, out var k) ? k : TaxonKind.Diet;
}
