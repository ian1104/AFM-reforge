using System.Text.Json;
using AFMReforge.Adapter.AFM;
using AFMReforge.Core;
using AFMReforge.Infrastructure;
using AlbionDataAvalonia.Network.Responses;

Console.WriteLine("AFM Reforge Step 8 mock persistence/query pipeline");

var databasePath = Path.Combine(AppContext.BaseDirectory, "afm-reforge-step7.db");
var store = new SqliteMarketObservationStore(databasePath);
var adapter = new AfmMarketAdapter();
var processing = new MarketProcessing(store: store);

adapter.MarketResponseObserved += processing.Process;

// Mock source: synthetic AFM response DTOs only. This is not Albion runtime data.
var mockResponses = new object[]
{
    new AuctionGetOffersResponse(new Dictionary<byte, object>
    {
        [0] = new[] { CreateMockOrderJson(1, "MOCK_OFFERS_ITEM") }
    }),
    new AuctionGetRequestsResponse(new Dictionary<byte, object>
    {
        [0] = new[] { CreateMockOrderJson(2, "MOCK_REQUESTS_ITEM") }
    }),
    new AuctionGetLoadoutOffersResponse(new Dictionary<byte, object>
    {
        [1] = new[] { new[] { CreateMockOrderJson(3, "MOCK_LOADOUT_ITEM") } }
    })
};

foreach (var response in mockResponses)
{
    switch (response)
    {
        case AuctionGetOffersResponse offers:
            adapter.ProcessMockResponse(offers);
            break;
        case AuctionGetRequestsResponse requests:
            adapter.ProcessMockResponse(requests);
            break;
        case AuctionGetLoadoutOffersResponse loadout:
            adapter.ProcessMockResponse(loadout);
            break;
    }
}

var persisted = store.ReadAll();
var recent = store.Query(new MarketRecordQuery(Limit: 3));
var itemMatches = store.Query(new MarketRecordQuery(ItemTypeId: "MOCK_REQUESTS_ITEM"));
var locationMatches = store.Query(new MarketRecordQuery(LocationId: "1001"));

Console.WriteLine($"Mock inputs processed: {processing.State.InputCount}");
Console.WriteLine($"SQLite records read back: {persisted.Count}");
Console.WriteLine($"Recent query records: {recent.Count}");
Console.WriteLine($"Item filter records: {itemMatches.Count}");
Console.WriteLine($"Location filter records: {locationMatches.Count}");
Console.WriteLine($"Database: {databasePath}");
Console.WriteLine("Runtime status: UNAVAILABLE");

static string CreateMockOrderJson(int id, string itemTypeId)
    => JsonSerializer.Serialize(new
    {
        Id = id,
        ItemTypeId = itemTypeId,
        ItemGroupTypeId = "MOCK_GROUP",
        LocationId = 1001,
        QualityLevel = 1,
        EnchantmentLevel = 0,
        UnitPriceSilver = 1234L,
        Amount = 2,
        AuctionType = "MOCK_AUCTION_TYPE",
        Expires = "2030-01-01T00:00:00Z",
        DistanceFee = 0,
        Location = "MOCK_LOCATION"
    });
