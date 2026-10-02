namespace AFMReforge.Core;

/// <summary>
/// Minimal application processing boundary: input -> optional persistence -> in-memory state.
/// No persistence policy, deduplication, pricing analysis, or final Observation semantics.
/// </summary>
public sealed class MarketProcessing
{
    private readonly MarketObservationState _state;
    private readonly IMarketObservationStore? _store;

    public MarketProcessing(
        MarketObservationState? state = null,
        IMarketObservationStore? store = null)
    {
        _state = state ?? new MarketObservationState();
        _store = store;
    }

    public MarketObservationState State => _state;

    public void Process(MarketObservationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        // Persistence is completed first so an I/O failure does not leave the
        // in-memory state ahead of durable local storage.
        _store?.Persist(input);
        _state.Append(input);
    }
}