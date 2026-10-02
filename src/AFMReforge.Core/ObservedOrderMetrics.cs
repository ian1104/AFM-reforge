namespace AFMReforge.Core;

public sealed record ObservedOrderMetrics(
    int Count,
    ulong? MinUnitPriceSilver,
    ulong? MaxUnitPriceSilver,
    decimal? AverageUnitPriceSilver,
    ulong TotalAmount,
    decimal TotalNotionalSilver);
