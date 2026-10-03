using AFMReforge.Infrastructure;
using AFMReforge.App;

var databasePath = Environment.GetEnvironmentVariable("AFM_REFORGE_DB")
    ?? Path.Combine(AppContext.BaseDirectory, "afm-reforge.db");

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls(Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? "http://localhost:5180");

var app = builder.Build();
var store = new SqliteMarketObservationStore(databasePath);

app.MapStaticAssets();
ReforgeUiEndpoints.Map(app, store);

app.MapGet("/api/status", () => Results.Ok(new
{
    application = "AFM Reforge",
    mode = "PRE-RUNTIME",
    runtimeIntegration = "PENDING VALIDATION",
    databasePath,
    head = "5b25a81ebb16981380d82d1233d0a138ff9b94bc"
}));

app.Run();
