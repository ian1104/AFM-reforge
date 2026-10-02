using AFMReforge.Core;

namespace AFMReforge.Core.Tests;

public sealed class MarketObservationInputTests
{
    [Fact]
    public void PreservesCapturedAtAndCanonicalRecords()
    {
        var captured = DateTimeOffset.UtcNow;
        var input = new MarketObservationInput(
            "AuctionGetOffersResponse",
            MarketResponseKind.Offers,
            null,
            captured,
            [new MarketRecord(
                1UL, "T4_BAG", "MOCK_GROUP", "1001", 1, 0, 1234UL, 2U,
                MarketOrderType.Unknown, "2030-01-01T00:00:00Z", 0UL)]);

        Assert.Equal(captured, input.CapturedAt);
        Assert.Single(input.Records);
        Assert.Equal("T4_BAG", input.Records[0].ItemTypeId);
    }
}
