# Step 9 Report — Observation / Snapshot Semantics Boundary

## 1. Implementation Summary

Step 9 adds a Reforge-owned Observation domain boundary without changing the existing SQLite schema or Step 8 query API.

Implemented:
- `MarketObservation` domain model
- `IMarketObservationFactory`
- runtime-independent `MarketObservationFactory`
- one explicit construction candidate per `MarketObservationInput`
- local `ObservationId` for the constructed candidate
- preservation of `ResponseType`, `ResponseKind`, `OperationCode`, `CapturedAt`, and orders
- in-memory tests for single response, multiple orders, ResponseKind, CapturedAt, duplicate Order IDs, and non-merging of separate responses
- Step 9 application demo that constructs and prints observation candidates from synthetic responses
- `docs/step9-report.md`

Not implemented:
- Observation SQLite table
- Snapshot table
- Observation-to-record persistence relation
- automatic deduplication
- cross-response grouping
- price history semantics
- market-history UI
- analysis or recommendation logic

## 2. Source Investigation

The existing AFM desktop integration was rechecked against the market response boundary.

The existing Offers handler receives an `AuctionGetOffersResponse` and passes its `value.marketOrders` collection to the existing market-order service. The Requests handler performs the same operation for `AuctionGetRequestsResponse`.

This establishes that the response object is a meaningful transport boundary containing a collection of market orders. It does **not** establish that the response is the final business-level Observation or Snapshot.

The current Reforge adapter also has typed handling for:
- `AuctionGetOffersResponse`
- `AuctionGetRequestsResponse`
- `AuctionGetLoadoutOffersResponse`

The adapter maps these into the same Reforge-owned `MarketObservationInput` boundary while preserving `MarketResponseKind`.

### Source findings

| Question | Status | Finding |
|---|---|---|
| Does an Offers response contain a collection of MarketOrder values? | CONFIRMED | Existing AFM Offers handler passes `value.marketOrders` onward. |
| Does a Requests response contain a collection of MarketOrder values? | CONFIRMED | Existing AFM Requests handler passes `value.marketOrders) onward. |
| Can one response contain multiple orders? | CONFIRMED | The response contract exposes an order collection; current Reforge mapping preserves all mapped orders. |
| Are ItemTypeId values guaranteed identical within one response? | UNVERIFIED | No inspected code establishes that invariant. |
| Are LocationId values guaranteed identical within one response? | UNVERIFIED | No inspected code establishes that invariant. |
| Are QualityLevel values guaranteed identical within one response? | UNVERIFIED | No inspected code establishes that invariant. |
| Are EnchantmentLevel values guaranteed identical within one response? | UNVERIFIED | No inspected code establishes that invariant. |
| Is there a response-level business identifier? | UNVERIFIED | Current Reforge input exposes OperationCode, ResponseType and CapturedAt, but no confirmed response identity. |
| Is CapturedAt propagated at the decoded response boundary? | CONFIRMED (static) | Existing AFM Core architecture propagates captured packet time into decoded operations consumed by typed handlers. |
| Is CapturedAt the official market observation time? | UNVERIFIED | Runtime evidence is still unavailable. |
| Are Offers and Requests automatically one logical Observation? | UNVERIFIED | No runtime evidence supports merging them. Step 9 does not merge them. |
| Are LoadoutOffers semantically identical to Offers? | UNVERIFIED | It is supported by the same Reforge adapter boundary, but final market semantics require runtime evidence. |
| Is one response definitely one final Observation? | UNVERIFIED | Step 9 implements this only as a construction candidate boundary. |
| Is a Snapshot required as a separate entity? | UNVERIFIED / not required now | No evidence currently requires cross-response aggregation into a Snapshot. |

## 3. Record / Observation / Snapshot Separation

The project now explicitly keeps three concepts separate:

### Stored Record

A single SQLite row representing the data persisted by the Step 7 storage boundary.

### Observation Candidate

A Reforge-owned domain object constructed from one `MarketObservationInput`.

The current construction rule is:

`MarketObservationInput -> one MarketObservation candidate`

This is an implementation boundary, not a claim about Albion's final market semantics.

### Snapshot

A possible future logical grouping of multiple records or observations representing a market state at a common point in time.

Step 9 does not implement Snapshot because the available static evidence does not establish that such cross-response grouping is correct.

Therefore:

`Stored Record != Observation`

and

`Observation candidate != confirmed Market Snapshot`

## 4. Observation Domain Model

Added:

`MarketObservation`

Fields:

- `ObservationId`
- `ResponseType`
- `ResponseKind`
- `OperationCode`
- `CapturedAt`
- `Orders`

### ObservationId

`ObservationId` is a locally generated `Guid` assigned by `MarketObservationFactory`.

Its current meaning is intentionally narrow:

> identity of the Reforge-created observation candidate instance.

It is **not**:
- an Albion server response ID
- a MarketOrder.Id
- a database StorageRecordId
- a deduplication key
- a proof that the response represents a final market observation

The factory generates a new identity for every construction call. This makes the distinction between Order identity and Observation candidate identity explicit without claiming that Albion provides an Observation ID.

Final Observation identity remains UNVERIFIED.

### ObservedAt

No `ObservedAt` field was introduced.

The reason is deliberate: there is currently no runtime evidence that `CapturedAt` can safely be renamed or promoted to the final market-observation timestamp.

### CapturedAt

`CapturedAt` is retained unchanged.

It means the captured timestamp carried through the current AFM/Reforge input path. Step 9 does not reinterpret it as an official market timestamp.

## 5. Observation Construction Boundary

Added:

`IMarketObservationFactory`

and:

`MarketObservationFactory`

The boundary is:

`MarketObservationInput -> MarketObservation`

The factory:
1. validates the input is non-null
2. creates a local candidate identity
3. preserves the response metadata
4. preserves the captured timestamp
5. preserves the complete order collection
6. performs no deduplication
7. performs no cross-response merge

This keeps domain semantics out of the SQLite implementation.

## 6. ResponseKind Semantics

Current values remain:

- `Offers`
- `Requests`
- `LoadoutOffers`

Step 9 preserves these as explicit metadata on the Observation candidate.

No automatic merge is performed:

`Offers + Requests != one merged Observation`

and:

`Offers + LoadoutOffers != one merged Observation`

This is a conservative construction policy. Whether these response types should later participate in a higher-level Snapshot remains UNVERIFIED.

## 7. Duplicate Policy

The Step 7–8 append-oriented policy remains unchanged.

If two inputs contain the same `MarketOrder.Id`:
- both input records can remain stored
- both can produce separate Observation candidates
- their Order IDs do not become Observation identity
- no DISTINCT, UPSERT, MERGE, or overwrite was added

Step 9 therefore keeps these concepts distinct:

`Order identity != Observation candidate identity != StorageRecordId`

Duplicate handling as a historical market-data policy remains deferred.

## 8. Timestamp Policy

The only timestamp used by the new domain object is:

`CapturedAt`

The implementation does not introduce:
- `ObservedAt`
- `MarketTime`
- `SnapshotTime`
- `ServerTime`

The distinction is intentional.

The current static architecture establishes that captured packet time is propagated through AFM Core into decoded operations. It does not establish, without live Albion runtime evidence, exactly how that time relates to the market response's semantic observation moment.

Therefore:

`CapturedAt != Automatically Confirmed Market Observation Time`

## 9. Snapshot Assessment

A separate Snapshot entity is **not implemented** in Step 9.

Reason:
- no confirmed runtime grouping rule exists
- no evidence establishes that multiple responses form one snapshot
- Offers and Requests cannot safely be merged
- Item/Location/Quality/Enchantment uniformity inside responses is not established
- a global market state must not be inferred from client-observed responses

A future Snapshot layer can be introduced after runtime evidence identifies a defensible grouping rule.

## 10. Tests

Added `tests/AFMReforge.Core.Tests/MarketObservationTests.cs`.

### Test A — Single response
One synthetic input produces one Observation candidate.

### Test B — Multiple orders
One input containing multiple orders keeps all orders under the same construction boundary.

### Test C — ResponseKind
Offers, Requests, and LoadoutOffers remain distinct.

### Test D — CapturedAt
Input CapturedAt is preserved exactly by construction.

The test intentionally does not call it an official market timestamp.

### Test E — Duplicate Order IDs
Two separate constructions containing the same Order ID receive different local Observation candidate identities.

### Test F — Different responses
Two separate inputs with the same ItemTypeId are not automatically merged.

All test data is explicitly synthetic.

## 11. Application Demo

`AFMReforge.App` now demonstrates:

`Synthetic AFM response`
→ `AFM Adapter`
→ `MarketObservationInput`
→ `MarketObservationFactory`
→ `Observation candidate output`

The existing Step 8 SQLite persistence/query demonstration remains in the application so the previous retrieval boundary is not discarded.

The observation portion does not create an Observation SQLite table.

Mock identifiers such as `MOCK_OFFERS_ITEM`, `MOCK_REQUESTS_ITEM`, and `MOCK_LOADOUT_ITEM` remain synthetic and are not presented as Albion market state.

## 12. SQLite / Query Preservation

No Observation table was added.

The existing Step 7 storage schema remains the storage of individual records.

The Step 8 query boundary remains unchanged:

`IMarketObservationQuery -> MarketRecordView`

No query API redesign was introduced.

This preserves:

`Stored Record != Observation`

and prevents SQLite schema decisions from defining the Observation domain prematurely.

## 13. Build / Test / CI / Runtime

Source implementation:
**COMPLETED**

Local build:
**UNAVAILABLE**

Local test:
**UNAVAILABLE**

CI build:
**UNVERIFIED**

CI test:
**UNVERIFIED**

Runtime:
**UNAVAILABLE**

No runtime-confirmed Observation semantics are claimed.

The current implementation is therefore source/static verified only. The new tests were added but were not executed locally because the required .NET SDK is unavailable in the current development environment.

## 14. Runtime-Confirmed Semantics

None.

The following remain unavailable for runtime confirmation:
- actual response grouping behavior
- actual response frequency
- duplicate response frequency
- duplicate MarketOrder frequency
- actual CapturedAt timing behavior
- whether a response corresponds to the complete client-visible market result
- whether multiple response types should form one higher-level Observation/Snapshot
- whether a separate Snapshot concept is useful
- final historical semantics

## 15. CONFIRMED / INFERRED / UNVERIFIED

### CONFIRMED
- Reforge receives three typed market response categories.
- Offers and Requests responses expose market-order collections to the existing AFM handling path.
- Reforge preserves all mapped orders in MarketObservationInput.
- ResponseKind is explicit.
- CapturedAt is available on the Reforge input boundary.
- Observation candidate construction can be kept independent of SQLite.
- Duplicate Order IDs are not used as Observation candidate identity.
- Step 7–8 storage/query structures remain intact.

### INFERRED
- A response-level construction boundary is a useful intermediate domain boundary because AFM already delivers the market data as a response containing an order collection.
- A local candidate identity is useful for distinguishing independently constructed domain instances.

These are design inferences, not claims about Albion server semantics.

### UNVERIFIED
- One response equals one final Observation.
- One response equals one final Snapshot.
- Response-level ItemTypeId uniformity.
- Response-level LocationId uniformity.
- Response-level QualityLevel uniformity.
- Response-level EnchantmentLevel uniformity.
- Response-level server/business identifier.
- CapturedAt as official market observation time.
- Cross-response merge rules.
- Cross-ResponseKind merge rules.
- Final historical deduplication policy.

## 16. Changed Files

Added:
- `src/AFMReforge.Core/MarketObservation.cs`
- `src/AFMReforge.Core/IMarketObservationFactory.cs`
- `tests/AFMReforge.Core.Tests/MarketObservationTests.cs`
- `docs/step9-report.md`

Updated:
- `src/AFMReforge.App/Program.cs`

Not changed by Step 9:
- Step 7 SQLite schema
- Step 8 query API
- AFM Core
- existing AFM Upload path
- existing Step 4–8 documentation

## 17. Git Safety

No destructive Git operation was used.

No:
- `git reset --hard`
- `git clean`
- `git checkout .`
- `git restore .`
- force push
- mass deletion
- mass overwrite

was used.

The Step 9 changes were added incrementally on top of the Step 8 head.

## 18. Next Decisions

The next semantics-dependent decisions should wait for runtime evidence where possible:

1. Determine whether a decoded market response corresponds to a complete client-visible result set or only one transport response unit.
2. Measure whether ItemTypeId, LocationId, QualityLevel, and EnchantmentLevel are invariant within each response.
3. Observe whether multiple responses belong to the same user action/query.
4. Determine whether repeated responses are refreshes, retries, pagination, or independent observations.
5. Determine the runtime meaning and precision of CapturedAt.
6. Decide whether Snapshot is needed after those observations.
7. Only then design Observation/Record persistence relations and final history semantics.

If runtime becomes available, the Step 4 Runtime Probe should be resumed before committing to final Observation/Snapshot semantics.

## 19. Core Principle

`Stored Record != Observation`

`Query Result != Market History`

`CapturedAt != Automatically Confirmed Market Observation Time`

The purpose of Step 9 is not to manufacture final market-history semantics from insufficient evidence. It is to establish a clean domain boundary so later runtime evidence can refine the model without making SQLite storage semantics the source of truth.
