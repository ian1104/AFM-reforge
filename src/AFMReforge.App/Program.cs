using AFMReforge.Core;

var records = new[]
{
    Create(100, "T6_SWORD", "CAERLEON", 4, 2, MarketOrderType.Offer, 100, 2),
    Create(101, "T6_SWORD", "CAERLEON", 4, 2, MarketOrderType.Offer, 120, 3),
    Create(102, "T6_SWORD", "CAERLEON", 4, 3, MarketOrderType.Offer, 200, 1),
    Create(103, "T6_SWORD", "BRIDGEWATCH", 4, 2, MarketOrderType.Offer, 150, 4),
    Create(104, "T6_SWORD", "CAERLEON", 4, 2, MarketOrderType.Request, 90, 5)
};

var scope = new MarketObservationScopeCalculator().Calculate(records);
var groups = MarketRecordGrouping.GroupByKey(records);
var metricsCalculator = new ObservedOrderMetricsCalculator();

Console.WriteLine("Observation Scope");
Console.WriteLine("-----------------");
Console.WriteLine($"Record Count: {records.Length}");
Console.WriteLine($"ItemTypes: {string.Join(", ", scope.ItemTypeIds)}");
Console.WriteLine($"Locations: {string.Join(", ", scope.LocationIds)}");
Console.WriteLine($"Qualities: {string.Join(", ", scope.QualityLevels)}");
Console.WriteLine($"Enchantments: {string.Join(", ", scope.EnchantmentLevels)}");
Console.WriteLine($"AuctionTypes: {string.Join(", ", scope.AuctionTypes)}");
Console.WriteLine($"HasMultipleItemTypes: {scope.HasMultipleItemTypes}");
Console.WriteLine($"HasMultipleLocations: {scope.HasMultipleLocations}");
Console.WriteLine($"HasMultipleQualities: {scope.HasMultipleQualities}");
Console.WriteLine($"HasMultipleEnchantments: {scope.HasMultipleEnchantments}");
Console.WriteLine($"HasMultipleAuctionTypes: {scope.HasMultipleAuctionTypes}");

Console.WriteLine();
Console.WriteLine("Grouping");
Console.WriteLine("--------");
Console.WriteLine($"Group count: {groups.Count}");

var index = 1;
foreach (var group in groups)
{
    var metrics = metricsCalculator.Calculate(group.Value);
    var key = group.Key;

    Console.WriteLine();
    Console.WriteLine($"Group {index++}");
    Console.WriteLine($"Key: Item={key.ItemTypeId}, Location={key.LocationId}, Quality={key.QualityLevel}, Enchantment={key.EnchantmentLevel}, AuctionType={key.AuctionType}");
    Console.WriteLine($"Record count: {group.Value.Count}");
    Console.WriteLine($"Observed Order Metrics: Count={metrics.Count}, Min={metrics.MinUnitPriceSilver}, Max={metrics.MaxUnitPriceSilver}, Average={metrics.AverageUnitPriceSilver}, TotalAmount={metrics.TotalAmount}, TotalNotional={metrics.TotalNotionalSilver}");
}

Console.WriteLine();
Console.WriteLine("Runtime status: UNAVAILABLE");

static MarketRecord Create(
    ulong orderId,
    string itemTypeId,
    string locationId,
    byte quality,
    byte enchantment,
    MarketOrderType auctionType,
    ulong unitPrice,
    uint amount)
    => new(
        orderId,
        itemTypeId,
        "MOCK_GROUP",
        locationId,
        quality,
        enchantment,
        unitPrice,
        amount,
        auctionType,
        "2030-01-01T00:00:00Z",
        0UL);
