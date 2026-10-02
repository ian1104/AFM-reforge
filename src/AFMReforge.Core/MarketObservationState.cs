namespace AFMReforge.Core;

public sealed class MarketObservationState
{
    private readonly List<MarketObservationInput> _inputs = [];

    public IReadOnlyList<MarketObservationInput> Inputs => _inputs;

    public void Append(MarketObservationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        _inputs.Add(input);
    }

    public int InputCount => _inputs.Count;
}
