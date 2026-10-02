namespace AFMReforge.Core;

public sealed record MarketObservationScope(
    IReadOnlyList<string> ItemTypeIds,
    IReadOnlyList<string> LocationIds,
    IReadOnlyList<byte> QualityLevels,
    IReadOnlyList<byte> EnchantmentLevels,
    IReadOnlyList<MarketOrderType> AuctionTypes)
{
    public bool HasMultipleItemTypes => ItemTypeIds.Count > 1;
    public bool HasMultipleLocations => LocationIds.Count > 1;
    public bool HasMultipleQualities => QualityLevels.Count > 1;
    public bool HasMultipleEnchantments => EnchantmentLevels.Count > 1;
    public bool HasMultipleAuctionTypes => AuctionTypes.Count > 1;
}
