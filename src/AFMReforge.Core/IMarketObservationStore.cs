namespace AFMReforge.Core;

public interface IMarketObservationStore
{
    void Persist(MarketObservationInput input);
}