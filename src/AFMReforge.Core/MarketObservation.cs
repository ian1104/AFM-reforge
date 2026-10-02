namespace AFMReforge.Core;

public sealed record MarketObservation(
    Guid ObservationId,
    string ResponseType,
    MarketResponseKind ResponseKind,
    object? OperationCode,
    DateTimeOffset? CapturedAt,
    IReadOnlyList<MarketRecord> Records);
