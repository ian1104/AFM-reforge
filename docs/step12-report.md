# Step 12 Report — Synthetic End-to-End Invariant Validation

## 1. Synthetic scenario design

Step 12 validates the currently implemented Reforge data path without claiming any new Albion runtime semantics.

The tested path is:

Synthetic AFM-style Response
→ AFM Adapter
→ MarketObservationInput
→ MarketObservationFactory
→ MarketObservation candidate
→ IMarketObservationStore
→ SQLite
→ IMarketObservationQuery
→ MarketRecordView

Scenarios covered:
1. Single Observation with multiple records.
2. Duplicate MarketOrder.Id within one Observation.
3. Same MarketOrder.Id across different Observations.
4. Same item/location across independent Observations.
5. Offers / Requests / LoadoutOffers isolation.
6. Legacy record with NULL ObservationId.
7. Combined ObservationId + ItemTypeId + QualityLevel query filtering.
8. Failed persistence rollback.
9. Sequential observations and StorageRecordId ordering.
10. Empty synthetic response.

All databases used by the new tests are temporary test databases. No existing project SQLite database is reset or modified by the tests.

## 2. End-to-end path

The main synthetic invariant tests use the Adapter ProcessMockResponse path rather than constructing only the final domain objects.

For each synthetic AFM response:

AFM response → AfmMarketAdapter.Map → MarketObservationInput → MarketProcessing.Process → MarketObservationFactory → Persist(MarketObservation) → SQLite → Query

This keeps the test close to the current production-facing application boundary while remaining explicitly synthetic.

## 3. Invariant list

Observation identity: each processed synthetic input receives its own ObservationId. Same MarketOrder.Id does not become ObservationId.

Storage identity: each persisted order receives an independent StorageRecordId. Same ObservationId + same OrderId therefore still produces separate storage records.

Order identity: MarketOrder.Id is not used as a uniqueness key. Duplicate OrderIds remain stored.

Observation association: Querying with ObservationId returns only records associated with that candidate.

Query composition: ObservationId can be combined with ItemTypeId and QualityLevel without replacing or bypassing the existing SQL-side predicates.

ResponseKind isolation: Offers, Requests, and LoadoutOffers remain separate Observation candidates and retain their corresponding ResponseKind on records.

Legacy compatibility: record-only persistence continues to use NULL ObservationId. A legacy/unassociated record can coexist with new Observation-associated records.

Transaction atomicity: a duplicate ObservationId failure occurs inside the observation persistence transaction. The failed operation leaves neither its candidate nor its new records behind, while an already persisted observation remains intact.

Sequential observations: four sequential synthetic observations preserve their independent ObservationIds and record associations. Recent query ordering remains StorageRecordId DESC.

Empty response: current implementation policy is MarketObservationInput with zero Orders → MarketObservation candidate is created → market_observations row is persisted → zero market_observation_records rows. This is an implementation contract only.

## 4. Test results

The following Step 12-specific tests were added in tests/AFMReforge.Infrastructure.Tests/SyntheticEndToEndInvariantTests.cs.

A — ProcessesSyntheticResponsesThroughAdapterProcessingPersistenceAndQuery
Checks Adapter → Processing → Observation → SQLite → Query, multiple records, duplicate OrderId, StorageRecordId uniqueness, ObservationId filtering, and ItemTypeId + QualityLevel composition.

B — PreservesSameOrderIdAcrossDifferentObservations
Checks distinct ObservationIds, same OrderId retention, and independent StorageRecordIds.

C — KeepsResponseKindsAndObservationAssociationsIsolated
Checks Offers, Requests, LoadoutOffers, ObservationId association, and ResponseKind query isolation.

D — LegacyRecordCanCoexistWithoutBeingIncludedInObservationQuery
Checks record-only persistence, NULL ObservationId, coexistence with new Observation-associated records, and ObservationId query isolation.

E — FailedPersistenceRollsBackOnlyCurrentObservationOperation
Checks intentional Observation insert failure, no partial second operation, and preservation of existing stable data.

F — SequentialObservationsPreserveRecentStorageOrder
Checks four sequential Observations, multiple records per Observation, ObservationId association, and Recent query ordering.

G — EmptySyntheticResponseCreatesObservationWithZeroRecords
Checks the current zero-order persistence contract.

## 5. Existing persistence tests

Existing Step 10 persistence relation tests remain in place. They continue to cover candidate persistence, multiple records, duplicate OrderIds, separate Observations, legacy input persistence, legacy schema migration, and transaction failure.

Step 12 does not replace those tests.

## 6. Query isolation

The synthetic end-to-end path explicitly combines ObservationId = A, ItemTypeId = X, QualityLevel = 1, Limit = 10 and verifies that only matching records from Observation A are returned.

The SQLite implementation continues to construct SQL predicates and execute the filter in the database. No Step 12 change introduced a load-all-then-filter implementation.

## 7. Transaction atomicity

The existing storage boundary remains atomic for one observation persistence operation. A deliberate duplicate ObservationId causes the operation to fail before the second candidate's records are committed. Previously persisted data remains intact.

No destructive database operation was used.

## 8. Legacy compatibility

Legacy/record-only persistence remains available through Persist(MarketObservationInput) and leaves ObservationId = NULL.

Step 12 confirms that this record can coexist with an Observation-associated record in the same temporary database. The ObservationId query does not implicitly include the legacy record.

## 9. Duplicate behavior

Synthetic tests confirm that Observation A can contain Record A1 / OrderId 100 and Record A2 / OrderId 100 with different StorageRecordIds, while Observation B can independently contain another OrderId 100 record.

No automatic deduplication was added.

This is synthetic persistence behavior, not a claim about actual Albion duplicate response behavior.

## 10. Empty response behavior

The current implementation creates an Observation candidate and persists the market_observations row even when its order collection is empty. No market_observation_records row is created.

This result is recorded as the current application/storage contract only. It does not establish whether Albion sends empty market responses or what such responses mean.

## 11. Application demo

src/AFMReforge.App/Program.cs now runs a temporary synthetic scenario and prints the number of Observation candidates, ObservationId / ResponseKind / order count, stored record count, filtered records for one Observation, duplicate OrderId preservation, legacy record preservation, Recent query order, query isolation result, and Runtime status.

The demo uses a temporary database and deletes only that temporary database when the process finishes. It does not represent synthetic data as Albion runtime data.

## 12. Important Step 12 correction

During source inspection, the existing Step 10 query implementation contained a column-order mismatch.

The SQL SELECT had placed ObservationId immediately after StorageRecordId, while MarketRecordView defines ObservationId as its final field.

The constructor mapping therefore did not match the SELECT order.

Step 12 corrected the Query SELECT order so that StorageRecordId, ResponseType, ResponseKind, ..., ResolvedLocation, ObservationId matches the MarketRecordView constructor.

This correction is required for the Query invariant and does not alter the intended public query model.

## 13. Local build / test

The current model execution environment does not provide a usable .NET 10 SDK/runtime for local compilation.

Local build: UNAVAILABLE
Local test: UNAVAILABLE

No local PASS is claimed.

## 14. CI

A GitHub Actions workflow exists for .NET 10 restore, solution build, and solution test.

Step 12 changes were pushed to the repository so CI can validate the actual project.

If no workflow run/status is returned, the classification remains:
CI build: UNVERIFIED
CI test: UNVERIFIED

No synthetic test result is represented as a CI PASS.

## 15. Runtime

Actual Albion runtime was not used.

Runtime: UNAVAILABLE

Step 12 synthetic tests do not change the Step 11 runtime status. No Albion market semantics were inferred from these tests.

## 16. CONFIRMED

Implementation-level:
- Synthetic AFM-style responses can traverse Adapter → Input → Observation candidate → SQLite → Query.
- One Observation candidate can have multiple MarketRecords.
- Duplicate MarketOrder.Id values can remain as separate records.
- The same OrderId can occur under different ObservationIds.
- ObservationId filtering isolates associated records.
- ObservationId can be composed with ItemTypeId and QualityLevel filters.
- Offers, Requests, and LoadoutOffers remain separate candidate/record contexts.
- Legacy NULL ObservationId records coexist with associated records.
- Failed observation persistence is transactionally atomic.
- Sequential persistence retains StorageRecordId DESC Recent ordering.
- Empty inputs currently produce a persisted Observation row with zero record rows.
- No deduplication or higher-level grouping was introduced.

Code correction:
- Query SELECT/MarketRecordView column ordering was corrected to match the declared read-model field order.

## 17. INFERRED

The current persistence and query boundaries are internally coherent for the implemented synthetic model.

Candidate A (MarketRecord.ObservationId) remains sufficient for the relationships tested by the current application contract.

These are implementation conclusions, not Albion runtime semantic conclusions.

## 18. UNVERIFIED

- Actual Albion response boundaries.
- Actual market interaction → response count.
- Runtime Offers/Requests/LoadoutOffers behavior.
- Runtime duplicate OrderId behavior.
- Runtime CapturedAt semantics.
- Whether one real response should represent one business-level Observation.
- Snapshot semantics.
- Final market-history semantics.
- Whether Candidate A remains appropriate after real runtime evidence.
- Local build/test results.
- CI results until an actual workflow run/status is returned.

## 19. Next-step candidates

The next development step should not automatically add market analysis.

Two evidence paths remain:
1. Obtain an actual Albion/AFM runtime environment and resume runtime probing.
2. If runtime remains unavailable, continue only with clearly bounded application/storage invariants or test hardening.

No Snapshot, MarketHistory, deduplication, arbitrage, ROI, or recommendation layer should be introduced solely because the synthetic tests pass.

## 20. Git safety

Step 4–11 history is preserved.

No destructive Git operation was used. No existing project SQLite database was reset. All Step 12 test databases are temporary.

## 21. Core principle

Synthetic correctness ≠ Runtime correctness
Record identity ≠ Order identity
Observation candidate ≠ Confirmed Market Observation

Step 12 validates internal consistency of the implemented Reforge pipeline without manufacturing Albion runtime semantics.
