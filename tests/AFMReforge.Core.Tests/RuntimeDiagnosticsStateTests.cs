using AFMReforge.Core;
using Xunit;

namespace AFMReforge.Core.Tests;

public sealed class RuntimeDiagnosticsStateTests
{
    [Fact]
    public void StartsUnknown()
    {
        var state = new RuntimeDiagnosticsState();

        var snapshot = state.Snapshot();

        Assert.Equal("UNKNOWN", snapshot.AfmCoreStatus);
        Assert.Equal("UNKNOWN", snapshot.ReceiverStatus);
        Assert.Equal("UNKNOWN", snapshot.LastResponseKind);
        Assert.Null(snapshot.LastResponseTime);
        Assert.Null(snapshot.LastObservationId);
        Assert.Null(snapshot.LastError);
    }

    [Fact]
    public void OnlyExplicitEventsChangeRuntimeState()
    {
        var state = new RuntimeDiagnosticsState();
        var capturedAt = DateTimeOffset.UtcNow;
        var observationId = Guid.NewGuid();

        state.MarkAfmCoreInitialized();
        state.MarkReceiverInitialized();
        state.MarkMarketResponse(MarketResponseKind.Offers, 47, capturedAt);
        state.MarkAdapterConversion(47);
        state.MarkObservation(observationId, 47);
        state.MarkPersistence(47);

        var snapshot = state.Snapshot();

        Assert.Equal("VERIFIED", snapshot.AfmCoreStatus);
        Assert.Equal("VERIFIED", snapshot.ReceiverStatus);
        Assert.Equal("Offers", snapshot.LastResponseKind);
        Assert.Equal(capturedAt, snapshot.LastResponseTime);
        Assert.Equal(47, snapshot.LastResponseRecordCount);
        Assert.Equal(47, snapshot.LastAdapterRecordCount);
        Assert.Equal(observationId, snapshot.LastObservationId);
        Assert.Equal(47, snapshot.LastObservationRecordCount);
        Assert.Equal(47, snapshot.LastPersistenceCount);
    }

    [Fact]
    public void ErrorIsExplicitlyRecorded()
    {
        var state = new RuntimeDiagnosticsState();

        state.MarkError(new InvalidOperationException("runtime probe failure"));

        Assert.Equal("runtime probe failure", state.Snapshot().LastError);
    }
}
