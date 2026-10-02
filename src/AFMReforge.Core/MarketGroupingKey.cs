namespace AFMReforge.Core;

public sealed record MarketGroupingKey(
    string ItemTypeId,
    string LocationId,
    byte QualityLevel,
    byte EnchantmentLevel,
    MarketOrderType AuctionType)
{
    public static MarketGroupingKey From(MarketRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        return new MarketGroupingKey(
            record.ItemTypeId,
            record.LocationId,
            record.QualityLevel,
            record.EnchantmentLevel,
            record.AuctionType);
    }
}
