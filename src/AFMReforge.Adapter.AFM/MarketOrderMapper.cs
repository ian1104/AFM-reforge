using System.Globalization;
using System.Reflection;
using AFMReforge.Core;

namespace AFMReforge.Adapter.AFM;

public sealed class MarketOrderMapper
{
    public MarketRecord Map(object order)
    {
        ArgumentNullException.ThrowIfNull(order);
        return new MarketRecord(
            ToUInt64(GetRequiredMember(order, "Id"), "Id"),
            GetRequiredString(order, "ItemTypeId"),
            GetRequiredString(order, "ItemGroupTypeId"),
            GetRequiredString(order, "LocationId"),
            ToByte(GetRequiredMember(order, "QualityLevel"), "QualityLevel"),
            ToByte(GetRequiredMember(order, "EnchantmentLevel"), "EnchantmentLevel"),
            ToUInt64(GetRequiredMember(order, "UnitPriceSilver"), "UnitPriceSilver"),
            ToUInt32(GetRequiredMember(order, "Amount"), "Amount"),
            ToMarketOrderType(GetRequiredMember(order, "AuctionType")),
            GetRequiredString(order, "Expires"),
            ToUInt64(GetRequiredMember(order, "DistanceFee"), "DistanceFee"));
    }

    private static object GetRequiredMember(object value, string name)
        => GetMember(value, name)
           ?? throw new InvalidOperationException($"AFM MarketOrder field '{name}' was null or unavailable.");

    private static string GetRequiredString(object value, string name)
        => GetRequiredMember(value, name).ToString() ?? string.Empty;

    private static ulong ToUInt64(object value, string name)
    {
        try { return Convert.ToUInt64(value, CultureInfo.InvariantCulture); }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
        { throw new InvalidOperationException($"AFM MarketOrder field '{name}' could not be mapped to UInt64.", ex); }
    }

    private static uint ToUInt32(object value, string name)
    {
        try { return Convert.ToUInt32(value, CultureInfo.InvariantCulture); }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
        { throw new InvalidOperationException($"AFM MarketOrder field '{name}' could not be mapped to UInt32.", ex); }
    }

    private static byte ToByte(object value, string name)
    {
        try { return Convert.ToByte(value, CultureInfo.InvariantCulture); }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
        { throw new InvalidOperationException($"AFM MarketOrder field '{name}' could not be mapped to Byte.", ex); }
    }

    private static MarketOrderType ToMarketOrderType(object value)
    {
        var raw = value.ToString();
        if (string.Equals(raw, "offer", StringComparison.OrdinalIgnoreCase)) return MarketOrderType.Offer;
        if (string.Equals(raw, "request", StringComparison.OrdinalIgnoreCase)) return MarketOrderType.Request;
        return MarketOrderType.Unknown;
    }

    private static object? GetMember(object value, string name)
    {
        var type = value.GetType();
        var property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
        if (property is not null) return property.GetValue(value);
        var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
        return field?.GetValue(value);
    }
}
