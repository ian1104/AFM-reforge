# AFM Reforge

AFM Reforge is a separate project from the legacy `ian1104/albion-market-helper`.

## Current Architecture

```text
Albion Online
      ↓
AFMDataClientCore
      ↓
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
Query
      ↓
Grouping
      ↓
ObservedOrderMetrics
      ↓
Pre-Runtime UI
```

The current UI is intentionally observation-centric. It does not commit unresolved Snapshot, History, Trend, recommendation, or global market semantics.

## Pre-Runtime UI

The App is an ASP.NET Core Minimal API host with a static HTML/JavaScript UI.

Run the App and open:

```text
http://localhost:5180/
```

The default database is:

```text
afm-reforge.db
```

Override it with:

```text
AFM_REFORGE_DB=/path/to/database
```

For development-only synthetic data, use a separate demo database:

```text
AFM_REFORGE_DEMO=1
```

The demo path is explicitly synthetic and is not enabled by default.

## UI Areas

- Overview
- Observations
- Observation Detail
- Groups
- Observed Metrics / captured-record visualization
- Data search and raw-ish record inspection

See `docs/pre-runtime-ui-report.md` for the current implementation and runtime-validation checklist.

## Runtime Status

Actual Albion runtime validation is still pending.

The next runtime gate is intended to be performed with Albion Online + AFMDataClientCore + Reforge in the PC-room environment.

The Step 4 runtime probe documentation remains under `docs/runtime-probe/`.
