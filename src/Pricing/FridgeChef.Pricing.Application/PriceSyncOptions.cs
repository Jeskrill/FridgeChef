namespace FridgeChef.Pricing.Application;

public sealed class PriceSyncOptions
{
    public const string Section = "Pricing:Sync";

    public int BatchSize { get; set; } = 20;
    public int MaxIngredientsPerRun { get; set; }
}
