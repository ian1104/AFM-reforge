using AFMReforge.Core;
using AFMReforge.Infrastructure;

namespace AFMReforge.App;

public static class DemoDataSeeder
{
    public static void SeedIfEmpty(SqliteMarketObservationStore store)
    {
        if (store.ReadObservations().Count > 0)
            return;

        var capturedAt = DateTimeOffset.UtcNow;
        var offers = new MarketObservation(
            Guid.NewGuid(),
            "DemoOffersResponse",
            MarketResponseKind.Offers,
            "DEMO",
            capturedAt,
            [
                Create(100, "T6_SWORD", "CAERLEON", 4, 2, MarketOrderType.Offer, 42000, 3),
                Create(101, "T6_SWORD", "CAERLEON", 4, 2, MarketOrderType.Offer, 45000, 5),
                Create(102, "T6_SWORD", "CAERLEON", 4, 3, MarketOrderType.Offer, 51000, 1),
                Create(103, "T6_SWORD", "BRIDGEWATCH", 4, 2, MarketOrderType.Offer, 47000, 4)
            ]);

        var requests = new MarketObservation(
            Guid.NewGuid(),
            "DemoRequestsResponse",
            MarketResponseKind.Requests,
            "DEMO",
            capturedAt.AddSeconds(1),
            [
                Create(200, "T6_SWORD", "CAERLEON", 4, 2, MarketOrderType.Request, 39000, 2),
                Create(201, "T6_SWORD", "BRIDGEWATCH", 4, 2, MarketOrderType.Request, 40000, 6)
            ]);

        store.Persist(offers);
        store.Persist(requests);
    }

    private static MarketRecord Create(
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
}
