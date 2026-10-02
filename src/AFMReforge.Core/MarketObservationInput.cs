namespace AFMReforge.Core;

/// <summary>
/// Reforge-owned representation of one AFM market response.
/// This is deliberately not the final MarketObservation domain model.
/// </summary>
public sealed record MarketObservationInput(
    string ResponseType,
    object? OperationCode,
    DateTimeOffset? CapturedAt,
    IReadOnlyList<MarketOrderInput> Orders);

public sealed record MarketOrderInput(
    object? Id,
    object? ItemTypeId,
    object? ItemGroupTypeId,
    object? LocationId,
    object? QualityLevel,
    object? EnchantmentLevel,
    object? UnitPriceSilver,
    object? Amount,
    object? AuctionType,
    object? Expires,
    object? DistanceFee,
    object? ResolvedLocation);
