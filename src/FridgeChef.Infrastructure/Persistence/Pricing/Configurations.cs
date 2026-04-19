using FridgeChef.Domain.Pricing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FridgeChef.Infrastructure.Persistence.Pricing;

internal sealed class RetailerConfiguration : IEntityTypeConfiguration<Retailer>
{
    public void Configure(EntityTypeBuilder<Retailer> builder)
    {
        builder.ToTable("retailers", "pricing");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id").UseIdentityAlwaysColumn();
        builder.Property(r => r.Code).HasColumnName("code").IsRequired();
        builder.Property(r => r.Name).HasColumnName("name").IsRequired();
        builder.Property(r => r.BaseUrl).HasColumnName("base_url").IsRequired();
        builder.Property(r => r.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.HasIndex(r => r.Code).IsUnique();
    }
}

internal sealed class RetailerProductConfiguration : IEntityTypeConfiguration<RetailerProduct>
{
    public void Configure(EntityTypeBuilder<RetailerProduct> builder)
    {
        builder.ToTable("retailer_products", "pricing");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").UseIdentityAlwaysColumn();
        builder.Property(p => p.RetailerId).HasColumnName("retailer_id");
        builder.Property(p => p.ExternalSku).HasColumnName("external_sku").IsRequired();
        builder.Property(p => p.Title).HasColumnName("title").IsRequired();
        builder.Property(p => p.Url).HasColumnName("url").IsRequired();
        builder.Property(p => p.Brand).HasColumnName("brand");
        builder.Property(p => p.PackageQuantityValue).HasColumnName("package_quantity_value");
        builder.Property(p => p.PackageUnitId).HasColumnName("package_unit_id");
        builder.Property(p => p.PackageWeightG).HasColumnName("package_weight_g");
        builder.Property(p => p.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.HasOne(p => p.Retailer).WithMany().HasForeignKey(p => p.RetailerId);
        builder.HasIndex(p => new { p.RetailerId, p.ExternalSku }).IsUnique();
    }
}

internal sealed class IngredientProductMatchConfiguration : IEntityTypeConfiguration<IngredientProductMatch>
{
    public void Configure(EntityTypeBuilder<IngredientProductMatch> builder)
    {
        builder.ToTable("ingredient_product_matches", "pricing");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id").UseIdentityAlwaysColumn();
        builder.Property(m => m.FoodNodeId).HasColumnName("food_node_id");
        builder.Property(m => m.RetailerProductId).HasColumnName("retailer_product_id");
        builder.Property(m => m.MatchType).HasColumnName("match_type");
        builder.Property(m => m.Score).HasColumnName("score");
        builder.Property(m => m.IsPrimary).HasColumnName("is_primary").HasDefaultValue(false);
        builder.HasOne(m => m.RetailerProduct).WithMany().HasForeignKey(m => m.RetailerProductId);
        builder.HasIndex(m => new { m.FoodNodeId, m.RetailerProductId }).IsUnique();
    }
}

internal sealed class PriceSnapshotConfiguration : IEntityTypeConfiguration<PriceSnapshot>
{
    public void Configure(EntityTypeBuilder<PriceSnapshot> builder)
    {
        builder.ToTable("price_snapshots", "pricing");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id").UseIdentityAlwaysColumn();
        builder.Property(s => s.RetailerProductId).HasColumnName("retailer_product_id");
        builder.Property(s => s.CapturedAt).HasColumnName("captured_at");
        builder.Property(s => s.Price).HasColumnName("price");
        builder.Property(s => s.PromoPrice).HasColumnName("promo_price");
        builder.Property(s => s.Currency).HasColumnName("currency");
        builder.Property(s => s.PricePerKg).HasColumnName("price_per_kg");
        builder.Property(s => s.PricePerL).HasColumnName("price_per_l");
        builder.Property(s => s.InStock).HasColumnName("in_stock");
        builder.HasOne(s => s.RetailerProduct).WithMany().HasForeignKey(s => s.RetailerProductId);
    }
}
