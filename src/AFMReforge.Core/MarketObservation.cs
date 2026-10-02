namespace AFMReforge.Core;

/// <summary>
/// A Reforge-owned observation candidate constructed from one market response input.
/// This represents a local grouping boundary, not a runtime-confirmed Albion market snapshot.
/// </summary>
public sealed record MarketObservation(
    Guid ObservationId,
    string ResponseType,
    MarketResponseKind ResponseKind,
    object? OperationCode,
    DateTimeOffset? CapturedAt,
    IReadOnlyList<MarketOrderInput> Orders);
