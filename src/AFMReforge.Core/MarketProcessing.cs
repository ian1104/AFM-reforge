namespace AFMReforge.Core;

/// <summary>
/// Application processing boundary: input -> Observation candidate -> optional persistence -> in-memory state.
/// The Observation remains a candidate; this class does not assert final market-history semantics.
/// </summary>
public sealed class MarketProcessing
{
    private readonly MarketObservationState _state;
    private readonly IMarketObservationStore? _store;
    private readonly IMarketObservationFactory _observationFactory;
    private readonly RuntimeDiagnosticsState? _diagnostics;

    public MarketProcessing(
        MarketObservationState? state = null,
        IMarketObservationStore? store = null,
        IMarketObservationFactory? observationFactory = null,
        RuntimeDiagnosticsState? diagnostics = null)
    {
        _state = state ?? new MarketObservationState();
        _store = store;
        _observationFactory = observationFactory ?? new MarketObservationFactory();
        _diagnostics = diagnostics;
    }

    public MarketObservationState State => _state;

    public MarketObservation Process(MarketObservationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var observation = _observationFactory.Create(input);
        _diagnostics?.MarkObservation(observation.ObservationId, observation.Records.Count);

        // Persist the complete candidate first so its ObservationId can be
        // associated with all generated records in one storage transaction.
        _store?.Persist(observation);
        _state.Append(input);

        return observation;
    }
}
