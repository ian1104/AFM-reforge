using System.Text.Json;
using AFMReforge.Adapter.AFM;
using AlbionDataAvalonia.Network.Responses;

namespace AFMReforge.Adapter.AFM.Tests;

public sealed class AfmMarketAdapterTests
{
    [Fact]
    public void MapsOffersResponseToReforgeOwnedInput()
    {
        var order = JsonSerializer.Serialize(new
        {
            Id = 42,
            ItemTypeId = "T4_BAG",
            ItemGroupTypeId = "bag",
            LocationId = 1001,
            QualityLevel = 1,
            EnchantmentLevel = 0,
            UnitPriceSilver = 1234L,
            Amount = 2,
            AuctionType = "Sell",
            Expires = "2030-01-01T00:00:00Z",
            DistanceFee = 0,
            Location = "Caerleon"
        });

        var response = new AuctionGetOffersResponse(new Dictionary<byte, object>
        {
            [0] = new[] { order }
        });

        var adapter = new AfmMarketAdapter();
        var input = adapter.Map(response);

        Assert.Equal(nameof(AuctionGetOffersResponse), input.ResponseType);
        var mapped = Assert.Single(input.Orders);
        Assert.Equal(42, mapped.Id);
        Assert.Equal("T4_BAG", mapped.ItemTypeId);
        Assert.Equal(1001, mapped.LocationId);
        Assert.Equal(1234L, mapped.UnitPriceSilver);
        Assert.Equal(2, mapped.Amount);
        Assert.Equal("Sell", mapped.AuctionType);
        Assert.Equal("Caerleon", mapped.ResolvedLocation);
    }

    [Fact]
    public void MapsRequestsResponse()
    {
        var response = new AuctionGetRequestsResponse(new Dictionary<byte, object>
        {
            [0] = Array.Empty<string>()
        });

        var input = new AfmMarketAdapter().Map(response);

        Assert.Equal(nameof(AuctionGetRequestsResponse), input.ResponseType);
        Assert.Empty(input.Orders);
    }
}
