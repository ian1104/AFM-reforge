# Step 8 Report — Query & History Retrieval Foundation

## 1. Summary

Step 8 adds a runtime-independent retrieval layer over the existing Step 7 SQLite records.

Implemented:
- separate IMarketObservationQuery query boundary
- MarketRecordQuery
- Reforge-owned MarketRecordView
- recent retrieval
- ItemTypeId filter
- LocationId filter
- QualityLevel filter
- EnchantmentLevel filter
- ResponseKind filter
- CapturedAt time-range filter
- limit / offset
- combined filters
- SQLite-side filtering, ordering, limit and offset
- duplicate MarketOrder.Id preservation
- application console query demonstration
- query tests A–H
- existing persistence tests retained

The query layer is explicitly a retrieval layer over stored records. It does not define final Observation, Snapshot, Session, Visit, or market-history semantics.

## 2. Actual Architecture

AFM Core → AFM Adapter → MarketObservationInput → MarketProcessing → IMarketObservationStore → SQLite → IMarketObservationQuery → MarketRecordView → Application / Future UI

The write and read boundaries are separated. Core does not reference SQLite.

## 3. Query API

Added IMarketObservationQuery.Query(MarketRecordQuery).

MarketRecordQuery supports ItemTypeId, LocationId, QualityLevel, EnchantmentLevel, ResponseKind, From, To, Limit, and Offset.

The four value filters are object? rather than string? because Step 7 deliberately preserved source values without prematurely fixing their domain types. This allows numeric LocationId/QualityLevel values to be queried using the same serialized representation that was stored.

Validation:
- Limit > 0
- Offset >= 0
- From <= To

## 4. Supported Filters

| Filter | Supported |
| --- | --- |
| Recent | Yes |
| Item | Yes |
| Location | Yes |
| Quality | Yes |
| Enchantment | Yes |
| ResponseKind | Yes |
| Time range | Yes |
| Limit | Yes |
| Offset | Yes |

Multiple filters are combined with SQL AND. Filtering, ordering, limit and offset are performed by SQLite.

## 5. Ordering

Results are ordered by StorageRecordId DESC.

Reason: StorageRecordId is the local SQLite insertion identity established in Step 7. This gives deterministic stored-record order without asserting that CapturedAt is a final observation timestamp.

CapturedAt is used only for explicit time-range filtering. The report therefore uses “stored-record order” rather than “latest observation”.

## 6. Read Model

Added MarketRecordView. Application/UI-facing query results do not expose SQLite reader types.

The read model contains only fields already present in Step 7 storage: StorageRecordId, ResponseType, ResponseKind, OperationCode, CapturedAt, OrderId, ItemTypeId, ItemGroupTypeId, LocationId, QualityLevel, EnchantmentLevel, UnitPriceSilver, Amount, AuctionType, Expires, DistanceFee, and ResolvedLocation.

CapturedAt is represented as DateTimeOffset? after round-trip parsing. This does not make it the final Observation time.

## 7. Duplicate Behavior

Duplicate behavior remains append-oriented. Two records with the same MarketOrder.Id remain separately queryable.

No DISTINCT MarketOrder.Id, UPSERT, MERGE, or overwrite behavior is applied. This verifies current persistence behavior; it does not establish final historical deduplication policy.

## 8. Query Tests

A — Recent: newest stored records first by StorageRecordId.

B — Item filter: requested ItemTypeId only.

C — Location filter: requested LocationId.

D — Combined filters: ItemTypeId + LocationId + QualityLevel + EnchantmentLevel + ResponseKind.

E — ResponseKind: Offers / Requests / LoadoutOffers separation.

F — Time range: CapturedAt range filtering.

G — Limit / Offset: database-side pagination parameters.

H — Duplicate Order IDs: repeated MarketOrder.Id records remain queryable.

Additional validation covers invalid limit, offset, and reversed time range.

Actual execution: UNVERIFIED. Local .NET SDK is unavailable and no GitHub Actions workflow run was returned for the Step 8 head during implementation.

## 9. Application Demo

AFMReforge.App now performs Mock responses → SQLite persistence → recent query → item filter → location filter.

This remains mock data and is not runtime evidence.

Actual execution: UNVERIFIED.

## 10. Build / Test / CI

Local build: UNAVAILABLE
Local test: UNAVAILABLE
CI build: UNVERIFIED
CI test: UNVERIFIED

The GitHub Actions workflow remains configured for restore/build/test. A workflow run/status was queried for the Step 8 head commit; no workflow run or combined status was returned through the available GitHub integration. No PASS is claimed.

## 11. Runtime

RUNTIME STATUS: UNAVAILABLE

No actual Albion runtime behavior was inferred from query tests or mock application.

## 12. Observation / History Semantics

Still unresolved:
- actual response grouping
- actual duplicate frequency
- actual CapturedAt semantics
- Observation definition
- Snapshot definition
- Market Session / Visit
- price-history interpretation
- final deduplication semantics

Stored Record ≠ Observation.
Query Result ≠ Market History.
CapturedAt ≠ confirmed Observation time.
Mock Data ≠ Real Albion Data.

## 13. Analysis

Not implemented: ROI, Profit, Spread, Price change, Trend, Moving average, Arbitrage, Recommendation, AI analysis.

Step 8 is retrieval only.

## 14. Database Safety

No existing database records were deleted or reset. Tests use independent temporary SQLite database files. No destructive database operation was introduced.

## 15. Changed Files

- src/AFMReforge.Core/IMarketObservationQuery.cs
- src/AFMReforge.Infrastructure/SqliteMarketObservationStore.cs
- tests/AFMReforge.Infrastructure.Tests/SqliteMarketObservationQueryTests.cs
- src/AFMReforge.App/Program.cs
- docs/step8-report.md

Step 4–7 files and documentation remain preserved.

## 16. Git

Step 8 implementation is committed and pushed to main. The final report is the repository head after this document is added. No destructive Git operation was used.

## 17. Completion Assessment

Retrieval:
- Stored records queryable: IMPLEMENTED
- Item filter: IMPLEMENTED
- Location filter: IMPLEMENTED
- Quality filter: IMPLEMENTED
- Enchantment filter: IMPLEMENTED
- ResponseKind filter: IMPLEMENTED
- Time range: IMPLEMENTED
- Limit / offset: IMPLEMENTED
- Read model: IMPLEMENTED
- Combined filters: IMPLEMENTED
- Duplicate records preserved: IMPLEMENTED

Scope:
- Final Observation semantics: NOT FINAL
- Final History semantics: NOT FINAL
- Final deduplication: NOT FINAL
- Analysis: NOT IMPLEMENTED
- UI: NOT IMPLEMENTED

Runtime: UNAVAILABLE

Verification:
- Source implementation: COMPLETED
- Local build/test: UNAVAILABLE
- CI: UNVERIFIED
- Runtime: UNAVAILABLE

## 18. Remaining Unknowns

1. Actual response grouping.
2. Actual duplicate behavior and frequency.
3. Actual CapturedAt semantics.
4. Observation definition.
5. Snapshot definition.
6. Final database/history semantics.
7. Whether the current order-row storage unit should later be supplemented by response/observation-level records.

## 19. Next-Step Rule

If actual Albion runtime becomes available, Step 4 Runtime Probe takes priority so runtime evidence can inform Observation and History semantics.

If runtime remains unavailable, runtime-independent application services, import/export, or UI-independent workflows can continue, while features requiring actual observation semantics remain deferred.

## 20. Core Principle

Step 8 makes existing SQLite records usable by application code. It does not claim to have completed the final Market History system.
