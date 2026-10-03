using AFMReforge.Core;
using AFMReforge.Infrastructure;
using AFMReforge.App;

var demoMode = string.Equals(Environment.GetEnvironmentVariable("AFM_REFORGE_DEMO"), "1", StringComparison.OrdinalIgnoreCase);
var databasePath = Environment.GetEnvironmentVariable("AFM_REFORGE_DB")
    ?? Path.Combine(AppContext.BaseDirectory, demoMode ? "afm-reforge-demo.db" : "afm-reforge.db");

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<RuntimeDiagnosticsState>();
builder.WebHost.UseUrls(Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? "http://localhost:5180");

var app = builder.Build();
var diagnostics = app.Services.GetRequiredService<RuntimeDiagnosticsState>();
var store = new SqliteMarketObservationStore(databasePath, diagnostics);
if (demoMode)
    DemoDataSeeder.SeedIfEmpty(store);

app.MapStaticAssets();
app.MapGet("/", () => Results.Redirect("/index.html"));
ReforgeUiEndpoints.Map(app, store);

app.MapGet("/api/status", () => Results.Ok(new
{
    application = "AFM Reforge",
    mode = "PRE-RUNTIME",
    runtimeIntegration = "PENDING VALIDATION",
    databasePath,
    preRuntimeBase = "5b25a81ebb16981380d82d1233d0a138ff9b94bc"
}));

app.Run();
