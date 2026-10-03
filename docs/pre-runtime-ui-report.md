# Pre-Runtime UI Completion Report

## Objective

Complete the runtime-independent UI and inspection workflow against the confirmed Reforge boundaries through Step 16.

The UI is intentionally centered on:

- Stored Observations
- Observed Records
- Observation Scope
- Grouping Keys
- Observed Order Metrics
- Raw-ish record inspection

It does not introduce Snapshot, History, Trend, Recommendation, or global market semantics.

## Current Architecture

```text
AFM Market Response
        ↓
AFM Reforge Adapter
        ↓
MarketRecord
        ↓
MarketObservation
        ↓
SQLite
        ↓
existing MarketRecordQuery / IMarketObservationQuery
        ↓
Minimal API
        ↓
static HTML / JavaScript UI
```

The UI host uses ASP.NET Core Minimal APIs and static web assets. This avoids adding a third-party UI framework to a repository that previously had no UI framework.

## Implemented

### Overview

Displays:

- Stored Observations
- Stored Records
- Known Item Types
- Known Locations
- Latest Captured At
- Stored response-kind counts
- Runtime boundary status

The values are explicitly scoped as stored/observed data.

### Observations

List:

- ObservationId
- CapturedAt
- ResponseKind
- Record count
- Scope summary

Detail:

- ObservationId
- ResponseKind
- CapturedAt
- Record count
- Observation Scope
- complete stored record fields

### Data

The existing SQLite query boundary is exposed through the UI.

Supported filters:

- ItemTypeId
- LocationId
- QualityLevel
- EnchantmentLevel
- ResponseKind
- ObservationId
- From
- To
- Limit
- Offset

No second client-side filtering implementation is used for the primary data query.

### Groups

The existing `MarketRecordGrouping.GroupByKey` is used.

The UI labels the result as `Grouping Key` and does not call it market identity or snapshot identity.

### Observed Metrics

The existing `ObservedOrderMetricsCalculator` is used for group analysis.

Displayed fields retain the observed scope:

- Count
- Minimum Observed Unit Price
- Maximum Observed Unit Price
- Average Observed Unit Price
- Total Observed Amount
- Total Observed Notional

### Observed Unit Price Visualization

A simple captured-record visualization is provided under:

`Observed Unit Price Over Captured Records`

This is only a visualization of stored observed records. It does not calculate or label a market trend.

### Runtime Diagnostics

A dedicated diagnostics boundary and UI were added without fabricating runtime state.

The diagnostics state starts as `UNKNOWN` and changes only when runtime integration components explicitly report events.

Tracked fields:

- AFM Core status
- Receiver status
- Last ResponseKind
- Last Response time
- Last Response record count
- Last Adapter conversion count
- Last ObservationId
- Last Observation record count
- Last persistence count
- Last error

`AfrmMarketAdapter.BuildConfiguredReceiver()` reports receiver initialization only after the receiver is actually built. Adapter conversion reports actual mapped record count. Observation processing and SQLite persistence report their actual event counts when the same diagnostics instance is supplied.

The current App host does not claim that AFM Core is connected because it does not establish an AFM runtime session by itself.

### Raw-ish Inspection

The Data and Observation Detail screens expose:

- StorageRecordId
- ObservationId
- OrderId
- ItemTypeId
- ItemGroupTypeId
- LocationId
- QualityLevel
- EnchantmentLevel
- UnitPriceSilver
- Amount
- AuctionType
- Expires
- DistanceFee
- CapturedAt
- response context

This is intended for the upcoming PC-room runtime validation.

## Demo Mode

The default UI does not seed fake data.

For development-only UI inspection:

```text
AFM_REFORGE_DEMO=1
```

uses a separate `afm-reforge-demo.db` and seeds clearly synthetic observations.

The demo mode is therefore isolated from the normal stored-data database.

## Empty State

With no observations, the UI displays an explicit empty state rather than fabricating records.

The default message identifies that runtime capture integration is still pending validation.

## Runtime Boundary

The UI does not infer:

- Offers = sell market
- Requests = buy market
- Observation = snapshot
- Average observed unit price = market price
- captured records = global market state
- CapturedAt = confirmed market timestamp semantics

These remain runtime-validation questions.

## Infrastructure Addition

`SqliteMarketObservationStore.ReadObservations()` was added as a read-only UI support boundary for the existing `market_observations` table.

The existing record Query API remains unchanged.

## Confirmed

- Repository remains separate from the legacy Albion Market Helper project.
- Step 4-16 history is preserved.
- Observation and record storage boundaries remain intact.
- Grouping continues to use the Step 15 grouping key.
- Metrics continue to use the Step 14 calculator.
- Scope continues to use the Step 16 scope calculator.
- UI reads through Reforge-owned persistence/query boundaries.
- No Snapshot/History/Trend semantics were added.

## Inferred

- ASP.NET Core Minimal APIs are a low-dependency fit because the repository previously had no UI framework.
- A static HTML/JavaScript client is sufficient for the current inspection and runtime-validation purpose.
- A separate demo database is safer than seeding the normal database.

## Unverified

- Local build/test execution in this environment.
- Actual browser rendering in this environment.
- Actual Albion runtime response contents.
- Exact runtime semantics of ResponseKind.
- Exact runtime semantics of CapturedAt.
- Actual observation boundaries across repeated requests.
- AFM/Reforge coexistence during live runtime.
- Real-world record cardinality and scope.

## PC-room Validation Checklist

When runtime becomes available:

1. Start Albion Online.
2. Start AFMDataClientCore and Reforge.
3. Confirm actual typed market responses reach the Reforge adapter.
4. Confirm each response produces the expected Observation candidate.
5. Inspect ObservationId and CapturedAt.
6. Inspect actual Record count and all raw-ish fields.
7. Check whether one response can contain multiple ItemTypeId / LocationId / Quality / Enchantment values.
8. Repeat the same market query and inspect whether separate observations are produced.
9. Verify AFM's existing upload path still coexists with Reforge.
10. Use the UI Data and Observation screens to capture the exact runtime behavior before adding higher-level market semantics.

## Verification Status

Local build: UNAVAILABLE in the current environment (the execution environment does not have the .NET SDK installed).

Local tests: UNAVAILABLE for the same reason.

CI build/test: no workflow run is currently reported for the latest phase commit.

Runtime: UNAVAILABLE; PC-room validation remains the runtime gate.

The diagnostics unit tests were added but could not be executed locally.

## Conclusion

The repository now has a usable pre-runtime inspection UI without committing unresolved market semantics.

The next meaningful gate is not additional market analysis. It is the actual Albion + AFM runtime connection and inspection of the data entering this UI.
