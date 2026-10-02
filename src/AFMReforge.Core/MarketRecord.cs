namespace AFMReforge.Core;

public enum MarketOrderType
{
    Unknown,
    Offer,
    Request
}

public sealed record MarketRecord(
    ulong OrderId,
    string ItemTypeId,
    string ItemGroupTypeId,
    string LocationId,
    byte QualityLevel,
    byte EnchantmentLevel,
    ulong UnitPriceSilver,
    uint Amount,
    MarketOrderType AuctionType,
    string Expires,
    ulong DistanceFee);
