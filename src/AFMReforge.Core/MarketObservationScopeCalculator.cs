namespace AFMReforge.Core;

public sealed class MarketObservationScopeCalculator
{
    public MarketObservationScope Calculate(IReadOnlyCollection<MarketRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);

        return new MarketObservationScope(
            records.Select(record => record.ItemTypeId).Distinct(StringComparer.Ordinal).ToArray(),
            records.Select(record => record.LocationId).Distinct(StringComparer.Ordinal).ToArray(),
            records.Select(record => record.QualityLevel).Distinct().ToArray(),
            records.Select(record => record.EnchantmentLevel).Distinct().ToArray(),
            records.Select(record => record.AuctionType).Distinct().ToArray());
    }
}
