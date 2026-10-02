namespace AFMReforge.Core;

public enum MarketResponseKind
{
    Offers,
    Requests,
    LoadoutOffers
}

/// <summary>
/// Reforge-owned transport/input shape for one AFM market response.
/// This is not the final MarketObservation domain model.
/// </summary>
public sealed record MarketObservationInput(
    string ResponseType,
    MarketResponseKind ResponseKind,
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
