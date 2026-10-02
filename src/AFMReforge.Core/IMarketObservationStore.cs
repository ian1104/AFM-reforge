namespace AFMReforge.Core;

public interface IMarketObservationStore
{
    /// <summary>
    /// Legacy record-only persistence boundary. Records persisted through this
    /// overload have no ObservationId association.
    /// </summary>
    void Persist(MarketObservationInput input);

    /// <summary>
    /// Persists an Observation candidate and all of its order records atomically.
    /// </summary>
    void Persist(MarketObservation observation);
}
