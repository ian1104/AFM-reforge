namespace AFMReforge.Core;

public interface IMarketObservationQuery
{
    IReadOnlyList<MarketRecordView> Query(MarketRecordQuery query);
}

public sealed record MarketRecordQuery(
    object? ItemTypeId = null,
    object? LocationId = null,
    object? QualityLevel = null,
    object? EnchantmentLevel = null,
    MarketResponseKind? ResponseKind = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    int Limit = 100,
    int Offset = 0);

public sealed record MarketRecordView(
    long StorageRecordId,
    string ResponseType,
    MarketResponseKind ResponseKind,
    string? OperationCode,
    DateTimeOffset? CapturedAt,
    string? OrderId,
    string? ItemTypeId,
    string? ItemGroupTypeId,
    string? LocationId,
    string? QualityLevel,
    string? EnchantmentLevel,
    string? UnitPriceSilver,
    string? Amount,
    string? AuctionType,
    string? Expires,
    string? DistanceFee,
    string? ResolvedLocation);
