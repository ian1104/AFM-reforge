namespace AFMReforge.Core;

/// <summary>
/// Explicit boundary for constructing a Reforge observation candidate from transport/input data.
/// The current implementation creates one candidate per input; runtime evidence is still required
/// before that grouping can be treated as the final market-observation semantics.
/// </summary>
public interface IMarketObservationFactory
{
    MarketObservation Create(MarketObservationInput input);
}

/// <summary>
/// Runtime-independent observation candidate construction.
/// </summary>
public sealed class MarketObservationFactory : IMarketObservationFactory
{
    public MarketObservation Create(MarketObservationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new MarketObservation(
            Guid.NewGuid(),
            input.ResponseType,
            input.ResponseKind,
            input.OperationCode,
            input.CapturedAt,
            input.Orders);
    }
}
