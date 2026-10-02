# Step 10 Report — Observation Candidate ↔ MarketRecord Persistence

## 1. Implementation Summary

Step 10 establishes an explicit persistence relationship between the Step 9 Reforge-owned `MarketObservation` candidate and the existing SQLite market records.

The selected model is:

`MarketObservation`
→ one-to-many
→ `market_observation_records.ObservationId`

with a separate:

`market_observations`

table containing only the minimum currently stable candidate metadata.

Implemented:
- observation-aware persistence overload on `IMarketObservationStore`
- atomic persistence of one Observation candidate and all of its order records
- `market_observations` table
- nullable `ObservationId` on `market_observation_records`
- non-destructive legacy schema upgrade for existing databases
- `ObservationId` in `MarketRecordView`
- `ObservationId` query filter
- persistence/association/duplicate/legacy/transaction tests
- Step 10 application demo
- `docs/step10-report.md`

Not implemented:
- Snapshot aggregation
- cross-response grouping
- Offers + Requests merge
- deduplication
- Order ID uniqueness rules
- final market-history semantics
- official observation timestamp semantics
- analysis/recommendation features

## 2. Existing Structure Investigation

The actual Step 9 structures were inspected before implementation.

### MarketObservation

Current candidate:

- `ObservationId: Guid`
- `ResponseType`
- `ResponseKind`
- `OperationCode`
- `CapturedAt`
- `Orders`

The identity is Reforge-local and represents the constructed candidate instance. It is not a server identifier.

### MarketObservationInput

Current input contains:
- response type
- response kind
- operation code
- captured timestamp
- order collection

It is the transport/application boundary before Observation construction.

### MarketRecordView

Current read model represents an individual stored SQLite record.

Step 10 adds only one relationship field:

`ObservationId: Guid?`

All existing record fields remain record-level fields.

### SQLite storage

The existing table is:

`market_observation_records`

with local:

`StorageRecordId INTEGER PRIMARY KEY AUTOINCREMENT`

and the existing response/order fields.

Before Step 10 there was no Observation relationship.

### Store

The existing store persisted one input's orders inside a transaction.

Step 10 retains that behavior and adds an observation-aware persistence path.

### Query

Step 8's query API remains intact. A single optional filter was added:

`MarketRecordQuery.ObservationId`

The existing filters, ordering, limit and offset remain unchanged.

## 3. CONFIRMED / INFERRED / UNVERIFIED

### CONFIRMED

- `MarketObservation` already existed as a Reforge-owned candidate.
- `MarketObservationInput` contains the order collection used to construct that candidate.
- `StorageRecordId` is the existing local SQLite record identity.
- Existing records are append-oriented and may contain repeated MarketOrder IDs.
- The store already used a transaction for multiple records from one input.
- SQLite can add a nullable column to an existing table without rewriting existing row contents; Step 10 uses that form for the legacy relationship column. SQLite documents `ADD COLUMN` as a supported ALTER TABLE operation. citeturn1search0
- A separate SQLite table can be created with `CREATE TABLE IF NOT EXISTS` without replacing an existing table. citeturn1search6

### INFERRED

- A direct nullable foreign-key-like identifier on each MarketRecord is the simplest representation for the current one-candidate-to-many-record relationship.
- Observation candidate metadata should be persisted separately rather than duplicated into every record.
- A nullable relationship is appropriate because existing records have no Observation candidate identity.

These are implementation inferences, not claims about final Albion market semantics.

### UNVERIFIED

- Whether one response is the final business-level Observation.
- Whether one response should always be persisted as one Observation candidate in future runtime semantics.
- Whether several responses should later belong to one Snapshot.
- Whether Offers, Requests, and LoadoutOffers should participate in a higher-level grouping.
- Whether `CapturedAt` is the official market observation time.
- Whether Observation-level ItemTypeId, LocationId, QualityLevel, or EnchantmentLevel should exist.
- Whether the final history model should use a different association structure.

## 4. Relationship Model Candidates

### Candidate A — MarketRecord.ObservationId

Structure:

`Observation`
→ `MarketRecord`
→ `MarketRecord`

Implementation:

`market_observations.ObservationId`

and:

`market_observation_records.ObservationId NULL`

#### Advantages

- Directly expresses the current one-to-many candidate relationship.
- Keeps `StorageRecordId` unchanged.
- Does not use `MarketOrder.Id` as an association key.
- Does not require a third association table.
- Allows existing legacy records to remain unassociated.
- Makes querying records for one candidate straightforward.
- Keeps record-level market fields on records.

#### Disadvantages

- The column assumes the current candidate relationship is one Observation candidate to many records.
- If future runtime evidence requires many-to-many grouping, the schema would need another evolution.
- The nullable relationship is intentionally weaker than a final historical semantic guarantee.

### Candidate B — Association table

Possible structure:

`Observation`
→ `ObservationRecords`
→ `MarketRecord`

This is more flexible for many-to-many relationships but introduces another persistence entity and another identity layer that current evidence does not require.

It would be premature at this stage.

### Candidate C — Application-only association

Possible structure:

`MarketObservation`
→ collection of `StorageRecordId`

This avoids database changes but loses the relationship across application restarts and makes querying the persisted relationship unnecessarily dependent on application memory.

It does not satisfy the goal of making the persisted relationship explicit.

### Decision

**Candidate A is selected.**

The current data flow naturally produces:

`1 Observation candidate -> N MarketRecord rows`

and there is no evidence requiring many-to-many association.

## 5. Observation Persistence Policy

Observation candidates are now persistable.

The persistence model is deliberately minimal:

### market_observations

- `ObservationId TEXT PRIMARY KEY`
- `CapturedAt TEXT NULL`
- `ResponseKind TEXT NOT NULL`

No Observation-level:
- ItemTypeId
- LocationId
- QualityLevel
- EnchantmentLevel
- price
- amount
- global market state

is stored.

Those values remain on the individual MarketRecord rows.

This avoids promoting currently unverified response-wide invariants into the database schema.

### Why ResponseType and OperationCode are not duplicated

The candidate already carries these values in memory, but Step 10 does not need to duplicate them into the Observation table to establish the relationship.

The existing MarketRecord rows retain response type and operation code.

This keeps the Observation persistence model minimal.

## 6. Record Relationship

Each record generated from an Observation candidate receives:

`ObservationId = observation.ObservationId`

Therefore:

`Observation A`
- `StorageRecord 1`
- `StorageRecord 2`
- `StorageRecord 3`

can all point to the same candidate identity.

The following identities remain distinct:

`StorageRecordId != ObservationId != MarketOrder.Id`

### Duplicate MarketOrder IDs

No uniqueness constraint was added to `OrderId`.

Therefore this remains valid:

`Observation A`
- `StorageRecord 1 / OrderId 100`
- `StorageRecord 2 / OrderId 100`

Both rows are retained.

## 7. Legacy Data Policy

Existing databases may contain:

`market_observation_records`

without `ObservationId`.

Step 10 does **not** assign historical Observation IDs to those rows.

Instead:

`ObservationId = NULL`

means:

> this stored record has no known Reforge Observation candidate association.

This avoids inventing historical grouping semantics.

### Migration

Initialization now:

1. creates `market_observations` if absent
2. creates the current record table if absent
3. checks the existing record table with `PRAGMA table_info`
4. adds nullable `ObservationId` only if the old schema lacks it

No existing rows are deleted or rewritten.

SQLite's documented ALTER TABLE support includes adding a column, and adding a nullable column does not require a fabricated value for existing rows. citeturn1search0

The implementation deliberately does not use:

`ALTER TABLE ... ADD COLUMN IF NOT EXISTS`

because SQLite's ALTER TABLE syntax does not provide that form; the code checks the existing schema first. citeturn1search0

## 8. Transaction Boundary

Observation-aware persistence uses one transaction:

```
BEGIN
    INSERT Observation
    INSERT Record 1
    INSERT Record 2
    ...
COMMIT
```

If any operation fails:

```
ROLLBACK
```

The transaction therefore prevents the newly introduced candidate from being persisted without its associated records, or vice versa.

The test suite uses duplicate Observation IDs to deliberately trigger a primary-key failure and verifies that the second candidate's partial records are not left behind.

This is a storage atomicity guarantee only. It does not establish any runtime market semantics.

## 9. Store API

The existing record-only persistence overload remains:

`Persist(MarketObservationInput)`

Its records have:

`ObservationId = NULL`

This preserves the existing low-level storage behavior.

A new overload was added:

`Persist(MarketObservation)`

This is the observation-aware persistence boundary.

The application processing path now follows:

`MarketObservationInput`
→ `MarketObservationFactory`
→ `MarketObservation`
→ `Persist(MarketObservation)`
→ `MarketRecord`

This ensures the same candidate identity is used for all records generated from that candidate.

## 10. Query Layer

The Step 8 query API was not redesigned.

Added only:

`MarketRecordQuery.ObservationId`

and:

`MarketRecordView.ObservationId`

The existing SQL-side:
- filters
- ordering
- limit
- offset
- time range
- ResponseKind

remain unchanged.

Example:

```
store.Query(new MarketRecordQuery(
    ObservationId: observation.ObservationId,
    Limit: 100))
```

returns the records associated with that candidate.

No deduplication is performed by this query.

## 11. Tests

Added:

`tests/AFMReforge.Infrastructure.Tests/SqliteMarketObservationRelationTests.cs`

### Test A — Observation persistence

An Observation candidate is persisted and its records can be read back with the same ObservationId.

### Test B — Multiple records

Multiple MarketRecords can belong to one Observation candidate.

### Test C — Association integrity

Queried records expose the expected ObservationId.

### Test D — Duplicate Order IDs

Two records with the same MarketOrder.Id can belong to one Observation and remain separate StorageRecords.

### Test E — Different observations

Two observations with otherwise similar market fields remain separate and are not merged.

### Test F — Transaction failure

A second persistence attempt using an existing ObservationId fails at the Observation insert and leaves no partial records from that second attempt.

### Test G — Legacy records

A database created with the pre-Step-10 schema is upgraded without assigning ObservationIds to existing records.

Existing Step 7–8 tests remain in place.

## 12. Application Demo

The Step 10 application path is:

```
Synthetic AFM response
        ↓
AFM Adapter
        ↓
MarketObservationInput
        ↓
MarketObservationFactory
        ↓
MarketObservation candidate
        ↓
SQLite persistence
        ↓
Query by ObservationId
        ↓
Linked MarketRecord output
```

The mock identifiers remain explicitly synthetic.

The application does not claim that the generated candidate is:
- a complete market snapshot
- server market state
- official market history

## 13. SQLite Safety

No destructive operation was used.

Not performed:
- DROP TABLE
- DELETE existing records
- database reset
- destructive migration
- table replacement
- mass data rewrite

The only legacy schema mutation is an additive nullable column when the column is absent.

## 14. Build / Test / CI / Runtime

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

The repository's current GitHub status/workflow query for the Step 10 HEAD must be treated as unverified unless an actual workflow run is returned.

No build or test PASS is claimed from source inspection alone.

## 15. Persistence Semantics Confirmed

Within the implemented Reforge storage model:

- An Observation candidate can have zero or more persisted MarketRecord rows.
- Each newly persisted candidate-generated MarketRecord receives the candidate's ObservationId.
- A StorageRecord retains its own StorageRecordId.
- Duplicate MarketOrder IDs remain independently stored.
- Legacy records can have NULL ObservationId.
- Observation candidate insertion and associated record insertion occur in one SQLite transaction.
- Observation-level persistence is intentionally minimal.

These are **implementation semantics**, not claims about Albion runtime semantics.

## 16. Still Unverified

The following remain explicitly unresolved:

- Observation candidate = actual Albion market Observation
- one response = one final Observation
- one response = one Snapshot
- response completeness
- response grouping across multiple packets
- refresh/retry/pagination interpretation
- Offers/Requests/LoadoutOffers higher-level grouping
- Observation timestamp semantics
- response-wide ItemType/Location/Quality/Enchantment invariants
- final market-history model
- whether Candidate A remains sufficient after runtime evidence

## 17. Runtime Semantics Boundary

Step 10 does not establish any of the following:

`Observation = official market snapshot`

`Observation = exact server market state`

`Observation = complete item market state`

`Observation = complete city market state`

The persisted Observation is still a **Reforge Observation candidate**.

## 18. Changed Files

Added:
- `tests/AFMReforge.Infrastructure.Tests/SqliteMarketObservationRelationTests.cs`
- `docs/step10-report.md`

Updated:
- `src/AFMReforge.Core/IMarketObservationStore.cs`
- `src/AFMReforge.Core/IMarketObservationQuery.cs`
- `src/AFMReforge.Core/MarketProcessing.cs`
- `src/AFMReforge.Infrastructure/SqliteMarketObservationStore.cs`
- `src/AFMReforge.App/Program.cs`

No AFM Core source was modified.

## 19. Git Safety

Step 4–9 history was preserved.

No:
- `git reset --hard`
- `git clean`
- `git checkout .`
- `git restore .`
- force push
- mass deletion
- mass overwrite

was used.

## 20. Next Decisions

When runtime becomes available, the next important validation is not more database functionality.

The priority is to determine:

1. What one actual market response represents.
2. Whether one response consistently forms one candidate grouping.
3. Whether multiple responses should remain separate or form a higher-level Snapshot.
4. Whether repeated Order IDs represent updates, repeated observations, or other transport behavior.
5. What `CapturedAt` means in actual runtime behavior.
6. Whether Candidate A remains adequate after those observations.

If runtime becomes available, Step 4 Runtime Probe should be resumed before introducing final Snapshot or Market History semantics.

## 21. Core Principle

`Observation candidate != Confirmed Market Observation`

`StorageRecordId != ObservationId != MarketOrder.Id`

`Legacy Record with NULL ObservationId != Record with invented historical Observation`

The Step 10 persistence layer records only the relationship that the current Reforge application can explicitly establish. It does not manufacture missing runtime semantics.
