using System.Text.Json;
using AFMReforge.Adapter.AFM;
using AFMReforge.Core;
using AFMReforge.Infrastructure;

var databasePath = Path.Combine(Path.GetTempPath(), $"afm-reforge-step14-demo-{Guid.NewGuid():N}.db");

try
{
    var store = new SqliteMarketObservationStore(databasePath);
    var adapter = new AfmMarketAdapter();
    var processing = new MarketProcessing(store: store);
    var observations = new List<MarketObservation>();

    adapter.MarketResponseObserved += input => observations.Add(processing.Process(input));
    adapter.ProcessMockResponse(CreateOffersResponse(
        (100, "MOCK_T6_SWORD", 2),
        (101, "MOCK_T6_SWORD", 3),
        (102, "MOCK_T6_SWORD", 5)));

    var records = observations.Single().Records;
    var metrics = new ObservedOrderMetricsCalculator().Calculate(records);

    Console.WriteLine("Observed Order Metrics");
    Console.WriteLine("----------------------");
    Console.WriteLine($"Count: {metrics.Count}");
    Console.WriteLine($"Min Unit Price: {metrics.MinUnitPriceSilver}");
    Console.WriteLine($"Max Unit Price: {metrics.MaxUnitPriceSilver}");
    Console.WriteLine($"Average Unit Price: {metrics.AverageUnitPriceSilver}");
    Console.WriteLine($"Total Amount: {metrics.TotalAmount}");
    Console.WriteLine($"Total Notional: {metrics.TotalNotionalSilver}");
    Console.WriteLine($"SQLite round-trip records: {store.Query(new MarketRecordQuery(ObservationId: observations.Single().ObservationId)).Count}");
    Console.WriteLine("Runtime status: UNAVAILABLE");
}
finally
{
    if (File.Exists(databasePath))
        File.Delete(databasePath);
}

static AuctionGetOffersResponse CreateOffersResponse(params (int Id, string ItemTypeId, int Amount)[] orders)
    => new(new Dictionary<byte, object>
    {
        [0] = orders.Select(CreateOrderJson).ToArray()
    });

static string CreateOrderJson((int Id, string ItemTypeId, int Amount) order)
    => JsonSerializer.Serialize(new
    {
        order.Id,
        order.ItemTypeId,
        ItemGroupTypeId = "MOCK_GROUP",
        LocationId = "1001",
        QualityLevel = 1,
        EnchantmentLevel = 0,
        UnitPriceSilver = order.Id == 100 ? 100L : order.Id == 101 ? 101L : 300L,
        order.Amount,
        AuctionType = "MOCK_AUCTION",
        Expires = "2030-01-01T00:00:00Z",
        DistanceFee = 0UL,
        Location = "MOCK_CAERLEON"
    });
