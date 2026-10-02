namespace AFMReforge.Core;

/// <summary>
/// Minimal application processing boundary: input -> in-memory state.
/// No persistence, deduplication, pricing analysis, or final Observation semantics.
/// </summary>
public sealed class MarketProcessing
{
    private readonly MarketObservationState _state;

    public MarketProcessing(MarketObservationState? state = null)
    {
        _state = state ?? new MarketObservationState();
    }

    public MarketObservationState State => _state;

    public void Process(MarketObservationInput input)
    {
        _state.Append(input);
    }
}
