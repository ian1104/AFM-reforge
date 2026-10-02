namespace AFMReforge.Core;

public static class MarketRecordGrouping
{
    public static IReadOnlyDictionary<MarketGroupingKey, IReadOnlyList<MarketRecord>> GroupByKey(
        IReadOnlyCollection<MarketRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);

        return records
            .GroupBy(MarketGroupingKey.From)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<MarketRecord>)group.ToArray());
    }
}
