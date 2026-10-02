using AFMReforge.Core;

namespace AFMReforge.Core.Tests;

public sealed class MarketObservationInputTests
{
    [Fact]
    public void PreservesCapturedAtAndOrders()
    {
        var captured = DateTimeOffset.UtcNow;
        var input = new MarketObservationInput("AuctionGetOffersResponse", null, captured,
            [new MarketOrderInput(1, "T4_BAG", null, 1001, 1, 0, 1234L, 2, "Sell", null, null, null)]);

        Assert.Equal(captured, input.CapturedAt);
        Assert.Single(input.Orders);
        Assert.Equal("T4_BAG", input.Orders[0].ItemTypeId);
    }
}
