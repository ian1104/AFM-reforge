using AFMReforge.Core;
using Xunit;

namespace AFMReforge.Core.Tests;

public sealed class RuntimeGateDiagnosticsTests
{
    [Fact]
    public void AllGatesStartUnknownAndRuntimeIsPending()
    {
        var state = new RuntimeDiagnosticsState();

        var snapshot = state.Snapshot();

        Assert.Equal("PENDING VALIDATION", snapshot.RuntimeSummary);
        Assert.Null(snapshot.FirstFailureBoundary);
        Assert.Equal(10, snapshot.Gates.Count);
        Assert.All(snapshot.Gates, gate => Assert.Equal("UNKNOWN", gate.Status));
    }

    [Fact]
    public void ExplicitSuccessOnlyPassesThatGate()
    {
        var state = new RuntimeDiagnosticsState();

        state.MarkGateSuccess(RuntimeGate.AppProcess);

        var snapshot = state.Snapshot();

        Assert.Equal("PASS", snapshot.Gates.Single(x => x.Gate == RuntimeGate.AppProcess).Status);
        Assert.Equal("UNKNOWN", snapshot.Gates.Single(x => x.Gate == RuntimeGate.HttpUi).Status);
        Assert.Equal("PENDING VALIDATION", snapshot.RuntimeSummary);
    }

    [Fact]
    public void FirstFailureBoundaryIsFirstFailedGateInPipelineOrder()
    {
        var state = new RuntimeDiagnosticsState();

        state.MarkGateSuccess(RuntimeGate.AppProcess);
        state.MarkGateSuccess(RuntimeGate.HttpUi);
        state.MarkGateSuccess(RuntimeGate.AfmCore);
        state.MarkGateFailure(RuntimeGate.Receiver, new InvalidOperationException("receiver failed"));

        var snapshot = state.Snapshot();

        Assert.Equal("FAIL", snapshot.RuntimeSummary);
        Assert.Equal(nameof(RuntimeGate.Receiver), snapshot.FirstFailureBoundary);
        Assert.Equal("FAIL", snapshot.Gates.Single(x => x.Gate == RuntimeGate.Receiver).Status);
        Assert.Equal("UNKNOWN", snapshot.Gates.Single(x => x.Gate == RuntimeGate.PacketCapture).Status);
        Assert.Equal("receiver failed", snapshot.Gates.Single(x => x.Gate == RuntimeGate.Receiver).LastError);
    }

    [Fact]
    public void RuntimeEventsAdvanceOnlyTheirOwnGate()
    {
        var state = new RuntimeDiagnosticsState();
        var capturedAt = DateTimeOffset.UtcNow;

        state.MarkAfmCoreInitialized();
        state.MarkReceiverInitialized();
        state.MarkMarketResponse(MarketResponseKind.Offers, 47, capturedAt);
        state.MarkAdapterConversion(47);
        state.MarkObservation(Guid.NewGuid(), 47);
        state.MarkPersistence(47);

        var snapshot = state.Snapshot();

        Assert.Equal("PASS", snapshot.Gates.Single(x => x.Gate == RuntimeGate.AfmCore).Status);
        Assert.Equal("PASS", snapshot.Gates.Single(x => x.Gate == RuntimeGate.Receiver).Status);
        Assert.Equal("PASS", snapshot.Gates.Single(x => x.Gate == RuntimeGate.MarketResponse).Status);
        Assert.Equal("PASS", snapshot.Gates.Single(x => x.Gate == RuntimeGate.Adapter).Status);
        Assert.Equal("PASS", snapshot.Gates.Single(x => x.Gate == RuntimeGate.Observation).Status);
        Assert.Equal("PASS", snapshot.Gates.Single(x => x.Gate == RuntimeGate.Persistence).Status);
        Assert.Equal("UNKNOWN", snapshot.Gates.Single(x => x.Gate == RuntimeGate.PacketCapture).Status);
        Assert.Equal("UNKNOWN", snapshot.Gates.Single(x => x.Gate == RuntimeGate.UiReflection).Status);
    }

    [Fact]
    public void SyntheticObservationDoesNotPassAllRuntimeGates()
    {
        var state = new RuntimeDiagnosticsState();

        state.MarkObservation(Guid.NewGuid(), 47);
        state.MarkPersistence(47);

        var snapshot = state.Snapshot();

        Assert.NotEqual("PASS", snapshot.RuntimeSummary);
        Assert.Equal("UNKNOWN", snapshot.Gates.Single(x => x.Gate == RuntimeGate.MarketResponse).Status);
        Assert.Equal("UNKNOWN", snapshot.Gates.Single(x => x.Gate == RuntimeGate.PacketCapture).Status);
        Assert.Equal("UNKNOWN", snapshot.Gates.Single(x => x.Gate == RuntimeGate.UiReflection).Status);
    }
}
