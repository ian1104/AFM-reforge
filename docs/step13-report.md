# AFM Reforge — Step 13 Report

## 1. Scope

Step 13 introduces a Reforge-owned canonical market record boundary:

`AFM MarketOrder -> AFM Adapter -> MarketRecord -> MarketObservation -> SQLite -> Query`

This step does not define Market History, ROI, Arbitrage, Recommendation, Snapshot semantics, or official Albion market state.

## 2. AFM MarketOrder source investigation

### CONFIRMED

The pinned AFM Core remains at submodule commit `e56a049be7c012ea37aa86367156007f64b148d1`.

The current AFM adapter consumes these response DTOs:

- `AuctionGetOffersResponse`
- `AuctionGetRequestsResponse`
- `AuctionGetLoadoutOffersResponse`

The response-side market order fields currently consumed at the adapter boundary are:

- `Id`
- `ItemTypeId`
- `ItemGroupTypeId`
- `LocationId`
- `QualityLevel`
- `EnchantmentLevel`
- `UnitPriceSilver`
- `Amount`
- `AuctionType`
- `Expires`
- `DistanceFee`
- `Location`

The exact defining `MarketOrder` source file is not part of the pinned AFMDataClientCore source tree. A public AlbionDataAvalonia-derived `MarketOrder` implementation was inspected as corroborating type evidence; it defines the fields as:

- `Id: ulong`
- `ItemTypeId: string`
- `ItemGroupTypeId: string`
- `LocationId: string`
- `QualityLevel: byte`
- `EnchantmentLevel: byte`
- `UnitPriceSilver: ulong`
- `Amount: uint`
- `AuctionType: enum`
- `Expires: string`
- `DistanceFee: ulong`
- `Location: AlbionLocation` computed from `LocationId`

The external source is treated as corroborating evidence, not as runtime confirmation.

## 3. Canonical MarketRecord

New type:

`AFMReforge.Core.MarketRecord`

Fields:

| Field | Canonical type | Nullability | Policy |
|---|---|---|---|
| OrderId | `ulong` | required | AFM/Albion-derived order identifier |
| ItemTypeId | `string` | required | preserved |
| ItemGroupTypeId | `string` | required | preserved |
| LocationId | `string` | required | raw location identifier |
| QualityLevel | `byte` | required | preserved |
| EnchantmentLevel | `byte` | required | preserved |
| UnitPriceSilver | `ulong` | required | raw silver-unit value; no normalization |
| Amount | `uint` | required | preserved |
| AuctionType | `MarketOrderType` | required | Reforge-owned enum |
| Expires | `string` | required | raw representation preserved |
| DistanceFee | `ulong` | required | raw value preserved |

`StorageRecordId`, `ObservationId`, and `CapturedAt` are not intrinsic MarketRecord fields.

Identity remains:

```
MarketRecord.OrderId   !=   StorageRecordId   !=   ObservationId
```

## 4. Type policy

The canonical type policy follows the inspected MarketOrder contract rather than converting everything to strings.

- Numeric identifiers/values remain numeric.
- Item and location identifiers remain strings.
- Quality/enchantment remain byte-sized values.
- AuctionType becomes a Reforge-owned `MarketOrderType`.
- Expires remains a string because Step 13 does not have sufficient runtime evidence to establish timestamp semantics.

No price conversion, tax calculation, fee calculation, or market-value normalization is performed.

## 5. Nullability policy

The canonical fields above are required.

The mapper rejects missing/null required fields instead of silently manufacturing values.

This is intentionally stricter than the current SQLite representation, where historical columns remain nullable for compatibility.

## 6. Location policy

Only `LocationId` enters MarketRecord.

The AFM `Location` computed object is not copied into the canonical model.

The existing SQLite `ResolvedLocation` column is retained for schema compatibility, but canonical MarketRecord persistence does not depend on that object.

No city-name normalization or assumed location mapping is introduced.

## 7. AuctionType policy

New Reforge-owned representation:

```
MarketOrderType.Unknown
MarketOrderType.Offer
MarketOrderType.Request
```

Known AFM values `offer` and `request` map to the corresponding Reforge enum values.

Unknown values map to `Unknown` rather than introducing an AFM enum dependency into Core.

No buy/sell economic interpretation is introduced at this step.

## 8. Expires policy

`Expires` is preserved as a required string.

No timestamp parsing, timezone conversion, expiration analysis, or historical interpretation is introduced.

This avoids assigning semantics that have not been runtime verified.

## 9. Mapper boundary

New:

`AFMReforge.Adapter.AFM.MarketOrderMapper`

Boundary:

```
AFM MarketOrder-shaped object
        |
        v
MarketOrderMapper
        |
        v
Reforge MarketRecord
```

`AfmMarketAdapter` now maps market orders through this mapper before producing `MarketObservationInput`.

Therefore Core receives:

```
MarketObservationInput
    -> IReadOnlyList<MarketRecord>
```

instead of the previous AFM-shaped `MarketOrderInput`.

## 10. Observation relationship

`MarketObservation` now contains:

```
IReadOnlyList<MarketRecord>
```

while:

- `ObservationId` remains observation identity.
- `CapturedAt` remains response/observation context.
- `MarketRecord.OrderId` remains market-order identity.

This keeps observation metadata separate from intrinsic market-record data.

## 11. Persistence compatibility

Existing tables remain:

- `market_observations`
- `market_observation_records`

No reset or destructive migration was performed.

SQLite storage continues to use the existing textual column representation and nullable historical schema.

Canonical numeric values are serialized through the existing storage mechanism. The Query API remains unchanged.

The existing `ResolvedLocation` column remains in the database schema for compatibility but is not populated from the AFM location object by the canonical mapper.

## 12. Query compatibility

The Step 8 query surface remains:

- ItemTypeId
- LocationId
- QualityLevel
- EnchantmentLevel
- ResponseKind
- From
- To
- Limit
- Offset
- ObservationId

The existing `MarketRecordView` shape remains unchanged.

During Step 13 inspection, the SQLite query projection was also corrected so its constructor arguments match the SELECT column order:

```
StorageRecordId
ResponseType
ResponseKind
OperationCode
CapturedAt
...
ObservationId
```

This avoids interpreting ResponseType as an ObservationId.

## 13. Tests

Added/updated coverage includes:

- AFM response -> canonical MarketRecord mapping.
- Numeric type preservation.
- AuctionType conversion.
- Required-field/nullability rejection.
- Observation metadata preservation.
- Duplicate OrderId preservation.
- Response-kind isolation.
- Legacy record coexistence.
- Transaction rollback.
- Sequential query ordering.
- Empty response behavior.
- Canonical MarketRecord -> SQLite -> MarketRecordView round trip.
- Existing query regression behavior.

Synthetic data continues to use `MOCK_*` markers.

## 14. Application demo

`AFMReforge.App` now demonstrates:

```
Mock AFM MarketOrder
        |
        v
AFM Adapter
        |
        v
Canonical MarketRecord
        |
        v
MarketObservation / SQLite
        |
        v
Query
        |
        v
Output
```

The demo does not expose AFM DTOs after the adapter boundary.

## 15. Architecture invariant

### CONFIRMED

```
AFM MarketOrder
    |
    v
Adapter boundary
    |
    v
Reforge MarketRecord
```

Core's project reference direction remains:

```
Adapter -> Core
Infrastructure -> Core
App -> Adapter/Core/Infrastructure
```

Core does not reference the AFM adapter project.

No AFM DTO type is included in `MarketRecord`, `MarketObservation`, or `MarketObservationInput`.

## 16. Build / Test / CI / Runtime

| Area | Status |
|---|---|
| Local build | UNAVAILABLE |
| Local test | UNAVAILABLE |
| CI build | UNVERIFIED |
| CI test | UNVERIFIED |
| Runtime | UNAVAILABLE |

The GitHub commit status for the current `main` head returned no statuses.

The workflow-run lookup for the current head returned no workflow runs.

Therefore no build/test PASS claim is made.

Runtime Albion market semantics remain unavailable.

## 17. Classification

### CONFIRMED

- AFM Core pinned commit remains `e56a049be7c012ea37aa86367156007f64b148d1`.
- Existing typed market response boundary remains in the adapter.
- Canonical MarketRecord is Reforge-owned.
- ObservationId, StorageRecordId, and OrderId remain separate identities.
- Location object is not copied into the domain model.
- Existing SQLite schema and Query API remain compatible.
- AFM DTO dependency stops at the adapter mapping boundary by source structure.
- Step 4~12 history remains preserved.

### INFERRED

- The inspected MarketOrder-derived field types are suitable canonical types for the current AFM integration.
- One canonical MarketRecord per AFM MarketOrder is the appropriate current record granularity.
- `Expires` should remain raw until runtime semantics are verified.
- `MarketOrderType.Unknown` is safer than exposing the AFM enum to Core.

### UNVERIFIED

- Actual live Albion MarketOrder values/types observed at runtime.
- Response completeness.
- Market completeness.
- Server-state semantics.
- CapturedAt semantics as market time.
- Observation grouping semantics.
- Snapshot semantics.
- Whether all future AFM MarketOrder variants will fit the current mapper without extension.

## 18. Next decisions

Before introducing Market History or analysis, runtime evidence should determine:

1. Whether the current MarketOrder field contract is stable across live responses.
2. Whether Expires has a reliable timestamp interpretation.
3. Whether Observation grouping remains response-level or requires another boundary.
4. Whether additional AFM market response types need canonical adapters.
5. Whether Query should eventually expose typed canonical records separately from the existing compatibility view.

No Market History, ROI, Arbitrage, Recommendation, price normalization, deduplication, or market-state inference was introduced.

## 19. Git safety

Step 4~12 commits were preserved.

No `git reset --hard`, `git clean`, `git checkout .`, `git restore .`, force push, mass deletion, or database reset was used.
