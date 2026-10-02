using AFMReforge.Adapter.AFM;

Console.WriteLine("AFM Reforge runtime host");
Console.WriteLine("Step 4 runtime status: UNAVAILABLE");
Console.WriteLine("Adapter foundation loaded; no live Albion capture is started in this environment.");

var adapter = new AfmMarketAdapter();
adapter.MarketResponseObserved += input =>
    Console.WriteLine($"Observed {input.ResponseType}: {input.Orders.Count} orders");
