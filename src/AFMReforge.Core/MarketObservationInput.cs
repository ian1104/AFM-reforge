namespace AFMReforge.Core;

public enum MarketResponseKind
{
    Offers,
    Requests,
    LoadoutOffers
}

public sealed record MarketObservationInput(
    string ResponseType,
    MarketResponseKind ResponseKind,
    object? OperationCode,
    DateTimeOffset? CapturedAt,
    IReadOnlyList<MarketRecord> Records);
