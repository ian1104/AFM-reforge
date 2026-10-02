using System.Reflection;
using AFMReforge.Core;
using Albion.Network;
using AlbionDataAvalonia.Network.Responses;

namespace AFMReforge.Adapter.AFM;

/// <summary>
/// AFM-specific boundary. AFM response DTOs stop here and are mapped to Reforge-owned data.
/// </summary>
public sealed class AfmMarketAdapter
{
    public event Action<MarketObservationInput>? MarketResponseObserved;

    public object BuildConfiguredReceiver(object afmCore)
    {
        var builder = ReceiverBuilder.Create();
        var register = afmCore.GetType().GetMethod("RegisterHandlers", BindingFlags.Instance | BindingFlags.Public)
            ?? throw new InvalidOperationException("AFM Core does not expose RegisterHandlers(builder).");
        register.Invoke(afmCore, [builder]);
        RegisterTypedMarketSubscriptions(builder);
        return builder.Build();
    }

    public void RegisterTypedMarketSubscriptions(object receiverBuilder)
    {
        Subscribe<AuctionGetOffersResponse>(receiverBuilder);
        Subscribe<AuctionGetRequestsResponse>(receiverBuilder);
        Subscribe<AuctionGetLoadoutOffersResponse>(receiverBuilder);
    }

    public MarketObservationInput Map<TResponse>(TResponse response)
        where TResponse : class
    {
        var kind = response switch
        {
            AuctionGetOffersResponse => MarketResponseKind.Offers,
            AuctionGetRequestsResponse => MarketResponseKind.Requests,
            AuctionGetLoadoutOffersResponse => MarketResponseKind.LoadoutOffers,
            _ => throw new ArgumentException($"Unsupported market response type: {response.GetType().FullName}", nameof(response))
        };

        var type = response.GetType();
        var ordersValue = GetMember(response, "marketOrders");
        var orders = ordersValue is System.Collections.IEnumerable enumerable
            ? enumerable.Cast<object>().Select(MapOrder).ToArray()
            : [];

        return new MarketObservationInput(
            type.Name,
            kind,
            GetMember(response, "OperationCode"),
            ToDateTimeOffset(GetMember(response, "CapturedAt")),
            orders);
    }

    private void Subscribe<TResponse>(object receiverBuilder)
        where TResponse : class
    {
        var methods = receiverBuilder.GetType()
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(m => m.Name == "SubscribeResponse" && m.IsGenericMethodDefinition)
            .Where(m => m.GetGenericArguments().Length == 1)
            .ToArray();

        if (methods.Length == 0)
            throw new InvalidOperationException("AFM ReceiverBuilder does not expose SubscribeResponse<T>.");

        var method = methods.FirstOrDefault(m => m.GetParameters().Length == 1)
            ?? throw new InvalidOperationException("AFM ReceiverBuilder SubscribeResponse<T> signature is unsupported.");
        var parameterType = method.GetParameters()[0].ParameterType;
        var callback = CreateCallbackDelegate(typeof(TResponse), parameterType);
        method.MakeGenericMethod(typeof(TResponse)).Invoke(receiverBuilder, [callback]);
    }

    private Delegate CreateCallbackDelegate(Type responseType, Type delegateType)
    {
        var invoke = delegateType.GetMethod("Invoke")
            ?? throw new InvalidOperationException("AFM response subscription parameter is not a delegate.");
        var parameters = invoke.GetParameters();
        if (parameters.Length != 1 || parameters[0].ParameterType != responseType)
            throw new InvalidOperationException("AFM response subscription delegate has an unexpected signature.");

        var methodName = invoke.ReturnType == typeof(Task) ? nameof(HandleAsync) : nameof(HandleSync);
        var method = GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!
            .MakeGenericMethod(responseType);
        return Delegate.CreateDelegate(delegateType, this, method);
    }

    private void HandleSync<TResponse>(TResponse response) where TResponse : class
        => MarketResponseObserved?.Invoke(Map(response));

    private Task HandleAsync<TResponse>(TResponse response) where TResponse : class
    {
        MarketResponseObserved?.Invoke(Map(response));
        return Task.CompletedTask;
    }

    private static MarketOrderInput MapOrder(object order)
    {
        return new MarketOrderInput(
            GetMember(order, "Id"),
            GetMember(order, "ItemTypeId"),
            GetMember(order, "ItemGroupTypeId"),
            GetMember(order, "LocationId"),
            GetMember(order, "QualityLevel"),
            GetMember(order, "EnchantmentLevel"),
            GetMember(order, "UnitPriceSilver"),
            GetMember(order, "Amount"),
            GetMember(order, "AuctionType"),
            GetMember(order, "Expires"),
            GetMember(order, "DistanceFee"),
            GetMember(order, "Location"));
    }

    private static object? GetMember(object value, string name)
    {
        var type = value.GetType();
        var property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
        if (property is not null) return property.GetValue(value);
        var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
        return field?.GetValue(value);
    }

    private static DateTimeOffset? ToDateTimeOffset(object? value)
    {
        if (value is DateTimeOffset dto) return dto;
        if (value is DateTime dt) return new DateTimeOffset(dt);
        return value is null ? null : DateTimeOffset.TryParse(value.ToString(), out var parsed) ? parsed : null;
    }
}
