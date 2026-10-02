namespace AFMReforge.Core;

public interface IMarketObservationFactory
{
    MarketObservation Create(MarketObservationInput input);
}

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
            input.Records);
    }
}
