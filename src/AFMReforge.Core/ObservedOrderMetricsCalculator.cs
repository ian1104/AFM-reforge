namespace AFMReforge.Core;

public sealed class ObservedOrderMetricsCalculator
{
    public ObservedOrderMetrics Calculate(IReadOnlyCollection<MarketRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);

        if (records.Count == 0)
            return new ObservedOrderMetrics(0, null, null, null, 0UL, 0m);

        ulong totalAmount = 0;
        decimal totalUnitPrice = 0m;
        decimal totalNotional = 0m;
        ulong minUnitPrice = ulong.MaxValue;
        ulong maxUnitPrice = ulong.MinValue;

        foreach (var record in records)
        {
            totalAmount = checked(totalAmount + record.Amount);
            totalUnitPrice += record.UnitPriceSilver;
            totalNotional += checked((decimal)record.UnitPriceSilver * record.Amount);

            if (record.UnitPriceSilver < minUnitPrice)
                minUnitPrice = record.UnitPriceSilver;
            if (record.UnitPriceSilver > maxUnitPrice)
                maxUnitPrice = record.UnitPriceSilver;
        }

        return new ObservedOrderMetrics(
            records.Count,
            minUnitPrice,
            maxUnitPrice,
            totalUnitPrice / records.Count,
            totalAmount,
            totalNotional);
    }
}
