# AFM Reforge — Step 15 Report

## 1. Grouping problem

Step 15 adds an explicit Reforge-owned boundary for deciding which canonical MarketRecord values belong to the same candidate comparable record set.

Existing arithmetic metrics remain collection-level. Grouping now has a separate responsibility:

    MarketRecord[] -> MarketGroupingKey -> grouped MarketRecord[] -> ObservedOrderMetrics

This does not establish an Albion market identity, market price, snapshot, or history model.

## 2. Current canonical input

MarketRecord contains OrderId, ItemTypeId, ItemGroupTypeId, LocationId, QualityLevel, EnchantmentLevel, UnitPriceSilver, Amount, AuctionType, Expires, and DistanceFee.

ObservationId, StorageRecordId, and CapturedAt remain response/storage context outside MarketRecord.

## 3. Candidate grouping dimensions

| Dimension | Status | Policy |
|---|---|---|
| ItemTypeId | CONFIRMED as available grouping dimension | Included |
| ItemGroupTypeId | CONFIRMED as available field; distinct from ItemTypeId in corroborating models | Not included in primary candidate key |
| LocationId | CONFIRMED as available grouping dimension | Included |
| QualityLevel | CONFIRMED as available grouping dimension | Included |
| EnchantmentLevel | CONFIRMED as available grouping dimension | Included |
| AuctionType | CONFIRMED as available grouping dimension | Included |

The key is a candidate Reforge grouping key, not a claim about the complete Albion market comparison identity.

## 4. ItemTypeId / ItemGroupTypeId investigation

Public Albion data models corroborate that ItemTypeId and ItemGroupTypeId are distinct fields. A public market-order representation shows a specific item identifier alongside a broader group identifier.

This supports treating ItemGroupTypeId as a potentially useful secondary analysis dimension rather than silently treating it as identical to ItemTypeId.

The Step 15 candidate key uses ItemTypeId because the immediate purpose is grouping records for the same specific canonical item. Including both fields would impose an additional equality constraint without evidence that it is required for the candidate comparable set.

The exact defining MarketOrder source for the pinned AFMDataClientCore commit was not independently available through the repository source index during this step. Therefore the relationship is classified as:

- AFM pinned-source field availability: CONFIRMED from the existing adapter contract/code
- ItemTypeId vs ItemGroupTypeId conceptual relationship: INFERRED from field semantics and public corroborating models
- Final Albion market identity: UNVERIFIED

## 5. Location policy

LocationId is included. Different LocationId values produce different candidate keys.

This is an internal grouping rule and does not assert that LocationId alone fully represents every possible market-location semantic.

## 6. Quality policy

QualityLevel is included. Different quality values produce different candidate keys.

## 7. Enchantment policy

EnchantmentLevel is included. Different enchantment values produce different candidate keys.

## 8. AuctionType policy

AuctionType is included. Offers and Requests therefore are not automatically placed in the same candidate group.

This prevents the Step 14 arithmetic metrics layer from silently combining different order-type categories when grouping is used.

The grouping layer preserves the canonical enum value; it does not assign further economic meaning to Offer versus Request.

## 9. OrderId policy

OrderId is excluded. It identifies an individual order and is not candidate comparable-set identity.

Duplicate OrderIds are retained as separate records. No deduplication occurs during grouping.

## 10. StorageRecordId policy

StorageRecordId is not part of MarketRecord and is not available to the pure grouping layer. It is local persistence identity, not candidate market grouping identity.

## 11. ObservationId policy

ObservationId is not part of MarketRecord, so the pure grouping API cannot use it as an implicit grouping dimension.

Step 15 therefore follows the no-implicit-scope policy: grouping supplied canonical records does not automatically separate them by ObservationId.

This does not make ObservationId a market identity. If a future use case requires grouping within one observation, that should be an explicit higher-level scope.

## 12. CapturedAt policy

CapturedAt is excluded. No minute/hour/day bucket is created.

Time-series semantics remain outside Step 15.

## 13. Null / missing policy

All fields currently used by MarketGroupingKey are non-nullable in canonical MarketRecord: ItemTypeId, LocationId, QualityLevel, EnchantmentLevel, and AuctionType.

Therefore no nullable grouping dimension requires a null-equality decision at this boundary. No null value is converted to an empty string.

If a future grouping dimension becomes nullable, its null/missing semantics must be explicitly defined before inclusion.

## 14. MarketGroupingKey

Implemented as a Reforge-owned record containing ItemTypeId, LocationId, QualityLevel, EnchantmentLevel, and AuctionType.

Equality is record value equality. Same field values produce equal keys and equivalent hash codes. Different included dimensions produce different keys.

## 15. Grouping API

Implemented: MarketRecordGrouping.GroupByKey(records).

Return shape:

    IReadOnlyDictionary<MarketGroupingKey, IReadOnlyList<MarketRecord>>

The implementation is pure in-memory grouping. No SQLite or SQL grouping logic was added.

## 16. Calculator relationship

ObservedOrderMetricsCalculator remains unchanged as a collection-level arithmetic calculator.

Step 15 enables:

    MarketRecord[]
        -> MarketRecordGrouping
        -> Group 1 -> ObservedOrderMetricsCalculator
        -> Group 2 -> ObservedOrderMetricsCalculator
        -> Group 3 -> ObservedOrderMetricsCalculator

No GroupedMetrics domain model was introduced because grouping correctness is the scope of this step.

## 17. Invariants

1. Every input record belongs to exactly one group.
2. No input record is deleted by grouping.
3. No input record is automatically deduplicated.
4. Equal grouping dimensions produce one equal key.
5. Different included grouping dimensions produce different keys.
6. Duplicate OrderIds remain separate records.
7. Storage identity does not affect grouping.
8. Observation context does not implicitly split groups.
9. Empty input produces zero groups.

## 18. Tests

MarketRecordGroupingTests covers same key, different ItemTypeId, LocationId, QualityLevel, EnchantmentLevel, AuctionType, duplicate OrderId, StorageRecordId-independent behavior, ObservationId-independent behavior at the canonical grouping boundary, empty input, record preservation, key value equality, and ItemGroupTypeId exclusion from the current candidate key.

Existing Step 14 metrics tests remain in place, including arithmetic behavior and overflow policy.

## 19. Application demo

The application demonstrates:

    MarketRecord[]
        -> Grouping
        -> Group count
        -> Group key
        -> Group record count
        -> Observed Order Metrics

The demo deliberately uses the label Observed Order Metrics and does not use Market Price or other market-wide terminology.

## 20. Runtime boundary

Actual Albion runtime remains unavailable.

Therefore grouping correctness demonstrated here applies to canonical synthetic MarketRecord values only.

The following remain UNVERIFIED: actual runtime distribution of grouping dimensions, actual Albion market comparison unit, actual market price level semantics, actual snapshot boundary, and actual history interval.

ItemTypeId + LocationId + QualityLevel + EnchantmentLevel + AuctionType is a candidate Reforge grouping key, not a confirmed complete Albion market identity.

## 21. Build / Test / CI / Runtime

- Local build: UNAVAILABLE
- Local test: UNAVAILABLE
- CI build: UNVERIFIED
- CI test: UNVERIFIED
- Runtime: UNAVAILABLE

No PASS claim is made without an actual build/test result.

## 22. CONFIRMED

- Current canonical MarketRecord fields are available to the Reforge grouping layer.
- MarketGroupingKey is Reforge-owned and independent of AFM DTOs.
- OrderId is excluded.
- StorageRecordId is excluded.
- ObservationId is not an implicit grouping dimension.
- CapturedAt is excluded.
- AuctionType is represented as a candidate grouping dimension.
- Grouping is pure in-memory logic.
- Existing metrics calculator remains compatible.
- Grouping tests cover the requested invariants.
- Step 4–14 history is preserved.

## 23. INFERRED

- ItemTypeId is the more appropriate primary specific-item dimension for the current candidate key.
- ItemGroupTypeId is better retained as a possible broader analysis dimension than forced into the primary key.
- Separating Offer and Request is a safer candidate boundary for later arithmetic analysis because their order semantics differ.

These are Reforge design inferences, not claims about the final Albion market model.

## 24. UNVERIFIED

- Whether the candidate key is the final market comparison identity.
- Whether additional Albion dimensions are required for a complete market comparison.
- Whether ItemGroupTypeId should become a future alternate grouping dimension.
- Runtime behavior of real observed records.

## 25. Next-step candidates

Possible later work, without being implemented here: explicit grouping scope over observations, alternate grouping dimensions for broader item-group analysis, grouped analysis models, and market history/time-series semantics.

Market Price, Trend, Moving Average, Spread, Arbitrage, ROI, and Recommendation remain outside this step.

## 26. Core principle

    Grouping Key
        !=
    Market Identity

    Comparable Record Set
        !=
    Market Snapshot

    Grouping correctness
        !=
    Market semantic correctness

Step 15 establishes an explicit internal boundary for candidate comparable record sets without prematurely claiming that the boundary represents the real Albion market.