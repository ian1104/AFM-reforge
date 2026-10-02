# Step 7 Report — Persistence Boundary & Runtime-Independent Storage Foundation

## 1. Summary

Step 7 implements a runtime-independent local persistence foundation for the Reforge-owned `MarketObservationInput`.

Implemented:

- `IMarketObservationStore` persistence boundary in Core
- `AFMReforge.Infrastructure` project
- SQLite initialization and local database creation
- append-oriented order-row persistence
- separate `StorageRecordId`
- explicit `MarketResponseKind` string serialization
- JSON serialization for object-valued input fields
- transaction per non-empty input
- read-back capability
- multiple-input / repeated `MarketOrder.Id` tests
- empty-input behavior
- App pipeline: Mock → Adapter → Core processing → SQLite

This does **not** finalize Market Observation semantics, Snapshot semantics, Query semantics, or deduplication policy.

## 2. Actual Architecture

```text
Mock Market Response
        ↓
AFM Adapter
        ↓
MarketObservationInput
        ↓
MarketProcessing
        ↓
IMarketObservationStore
        ↓
AFMReforge.Infrastructure
        ↓
SQLite
```

The existing Step 6 in-memory state is still maintained. Persistence is performed before the in-memory append so a storage exception does not leave the in-memory state ahead of local storage.

## 3. Infrastructure Project

Created:

```text
src/AFMReforge.Infrastructure/
├── AFMReforge.Infrastructure.csproj
└── SqliteMarketObservationStore.cs
```

Role:

- SQLite connection and initialization
- persistence mapping
- transaction handling
- read-back

Dependencies:

```text
AFMReforge.App
    ↓
AFMReforge.Infrastructure
    ↓
AFMReforge.Core

AFMReforge.Infrastructure
    ↓
Microsoft.Data.Sqlite
```

Core has no reference to SQLite or Infrastructure.

## 4. Database

SQLite is used.

App database path:

```text
<AppContext.BaseDirectory>/afm-reforge-step7.db
```

Table:

```text
market_observation_records
```

The table is deliberately raw-ish. It stores the current Reforge-owned input fields without derived market-analysis fields.

Initialization:

- open/create SQLite database
- `CREATE TABLE IF NOT EXISTS`
- no production migration framework

No ROI, profit, spread, trend, moving average, recommendation, or arbitrage fields are stored.

## 5. Persistence Model

| Field | Storage Type | Source | Status |
| --- | --- | --- | --- |
| StorageRecordId | INTEGER | SQLite generated | Local storage identity |
| ResponseType | TEXT | MarketObservationInput | Stored |
| ResponseKind | TEXT | MarketResponseKind | Stored as enum name |
| OperationCode | TEXT | MarketObservationInput | JSON serialized |
| CapturedAt | TEXT | MarketObservationInput | ISO-8601 round-trip format |
| OrderId | TEXT | MarketOrderInput.Id | JSON serialized |
| ItemTypeId | TEXT | MarketOrderInput.ItemTypeId | JSON serialized |
| ItemGroupTypeId | TEXT | MarketOrderInput.ItemGroupTypeId | JSON serialized |
| LocationId | TEXT | MarketOrderInput.LocationId | JSON serialized |
| QualityLevel | TEXT | MarketOrderInput.QualityLevel | JSON serialized |
| EnchantmentLevel | TEXT | MarketOrderInput.EnchantmentLevel | JSON serialized |
| UnitPriceSilver | TEXT | MarketOrderInput.UnitPriceSilver | JSON serialized |
| Amount | TEXT | MarketOrderInput.Amount | JSON serialized |
| AuctionType | TEXT | MarketOrderInput.AuctionType | JSON serialized |
| Expires | TEXT | MarketOrderInput.Expires | JSON serialized |
| DistanceFee | TEXT | MarketOrderInput.DistanceFee | JSON serialized |
| ResolvedLocation | TEXT | MarketOrderInput.ResolvedLocation | JSON serialized |

### Serialization decision

`MarketResponseKind` is stored using its symbolic name rather than its integer enum value. This avoids making persisted meaning depend on the current numeric enum ordering.

The object-valued fields are stored as JSON text rather than blindly coercing them to a primitive SQLite type. This keeps the current transport shape flexible while avoiding a premature domain-type decision.

This is a storage representation decision, not a final domain schema decision.

## 6. Identity / Deduplication

```text
Database identity:
    StorageRecordId — SQLite generated local identity

MarketOrder.Id:
    Stored as data, not used as database primary key

Deduplication:
    NOT FINAL
```

No unique constraint, overwrite, ignore, merge, or update behavior is applied based on `MarketOrder.Id`.

Repeated Order IDs are therefore currently storable.

This is append-oriented persistence for the current implementation, not a final historical-storage policy.

## 7. Transaction Boundary

For a non-empty `MarketObservationInput`:

```text
Input
 ├── Order 1
 ├── Order 2
 └── Order 3
       ↓
   one SQLite transaction
       ↓
     COMMIT
```

All order rows belonging to that processing call are committed together.

This is only a technical persistence transaction boundary. It does **not** establish that those orders form one domain Observation.

Empty input is a no-op because the current storage unit is an order row. No response-level record is created for an input containing zero orders.

## 8. Read-back Verification

Implemented test:

```text
Create input
    ↓
Persist
    ↓
ReadAll()
    ↓
Compare stored fields
```

The test checks:

- StorageRecordId
- ResponseType
- ResponseKind
- OrderId
- ItemTypeId
- LocationId
- QualityLevel
- EnchantmentLevel
- UnitPriceSilver
- Amount
- AuctionType
- Expires
- DistanceFee
- ResolvedLocation
- CapturedAt

Actual execution status is **UNVERIFIED** because the current development environment has no .NET SDK and no GitHub Actions run result was returned.

## 9. Multiple Input Verification

The test persists three inputs:

```text
Offers       / OrderId 1
Requests     / OrderId 1
LoadoutOffers/ OrderId 2
```

Expected storage behavior is three records with distinct local `StorageRecordId` values.

The repeated `MarketOrder.Id = 1` is intentionally retained.

Actual execution status: **UNVERIFIED**.

## 10. Empty Input Behavior

Current implementation:

```text
MarketObservationInput with zero Orders
        ↓
no SQLite row inserted
```

Reason:

- current storage row represents an input order
- final Observation/Response record semantics are not yet defined
- runtime evidence is unavailable

This behavior is therefore an implementation choice for Step 7, not a final domain decision.

## 11. Build / Test / CI

```text
Local build: UNAVAILABLE — dotnet SDK is not installed in the current environment
Local test:  UNAVAILABLE — dotnet SDK is not installed in the current environment

CI build: UNVERIFIED
CI test:  UNVERIFIED
```

GitHub Actions workflow remains configured for:

- restore
- Release build
- Release test

No workflow run was exposed for the Step 7 head commit, and no PASS is claimed.

## 12. Runtime

```text
RUNTIME STATUS: UNAVAILABLE
```

SQLite creation/persistence is not runtime verification.

No claim is made that actual Albion market responses have been successfully ingested.

## 13. Observation Semantics

Still intentionally unresolved:

- Observation
- Snapshot
- Query
- Deduplication policy
- Runtime grouping
- actual response frequency
- actual response ordering
- actual duplicate pattern
- background market requests
- runtime CapturedAt semantics

`CapturedAt` is preserved from the Reforge-owned input into SQLite, but it is **not** declared the final observation timestamp.

## 14. Changed Files

```text
AFMReforge.sln
src/AFMReforge.App/AFMReforge.App.csproj
src/AFMReforge.App/Program.cs
src/AFMReforge.Core/IMarketObservationStore.cs
src/AFMReforge.Core/MarketProcessing.cs
src/AFMReforge.Infrastructure/AFMReforge.Infrastructure.csproj
src/AFMReforge.Infrastructure/SqliteMarketObservationStore.cs
tests/AFMReforge.Infrastructure.Tests/AFMReforge.Infrastructure.Tests.csproj
tests/AFMReforge.Infrastructure.Tests/SqliteMarketObservationStoreTests.cs
```

Step 4 runtime-probe documentation and Step 5/6 implementation were preserved.

## 15. Git

Step 7 changes are committed and pushed to `main`.

Head commit:

```text
52afa10709e51fe4163663140c7cac5b301ce252
```

The Step 7 change set is 11 commits ahead of the Step 6 report commit:

```text
337f3bc70ef5c946aa4a3879181cc1c99e96ec8e
        ↓
52afa10709e51fe4163663140c7cac5b301ce252
```

No destructive Git operation was used.

CI result remains:

```text
UNVERIFIED
```

## 16. Remaining Unknowns

The main unresolved items remain runtime-dependent:

1. What constitutes a meaningful market observation.
2. Whether one response maps to one observation or another grouping.
3. How often identical MarketOrder IDs recur in actual runtime traffic.
4. Why repeated responses occur.
5. Whether background requests produce market data independently of visible UI actions.
6. Exact runtime semantics and precision of CapturedAt.
7. Whether later historical storage should remain order-row oriented or introduce response/observation-level records.
8. Final deduplication and historical identity rules.
9. Query semantics for historical retrieval.

## 17. Step 7 Completion Assessment

### Architecture

- Persistence boundary: **IMPLEMENTED**
- Core → SQLite direct reference: **NOT PRESENT**
- Infrastructure owns persistence: **IMPLEMENTED**

### Storage

- SQLite initialization: **IMPLEMENTED**
- Insert: **IMPLEMENTED**
- Read-back: **IMPLEMENTED**
- Multiple input: **IMPLEMENTED**
- Empty input: **IMPLEMENTED**

### Data integrity

- Main market fields: **MAPPED/STORED**
- CapturedAt: **PRESERVED**
- ResponseKind: **PRESERVED**
- MarketOrder.Id vs database identity: **SEPARATED**

### Scope

- Final Observation semantics: **NOT FINAL**
- Final deduplication policy: **NOT FINAL**
- Analysis: **NOT IMPLEMENTED**
- UI: **NOT IMPLEMENTED**

### Runtime

- Runtime result: **RUNTIME UNAVAILABLE**

### Verification

- Source-level implementation completed
- Local build/test execution unavailable
- CI result unavailable
- Therefore no build/test PASS is claimed

## 18. Core Principle

Step 7 does not create a finished Market Database.

It creates:

> a technical foundation for safely preserving the currently confirmed Reforge-owned market input in local storage.

The following distinctions remain explicit:

```text
Mock data
≠
Real market data

Storage success
≠
Runtime ingestion success

Database schema
≠
Final domain semantics
```
