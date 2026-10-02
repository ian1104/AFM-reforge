# Step 16 Report

## Objective

Step 16 makes the scope of the current Reforge Observation explicit without promoting it to a market snapshot.

The implemented boundary is:

    MarketRecord[] -> MarketObservationScopeCalculator -> MarketObservationScope

Scope is descriptive metadata about which dimension values are present in the supplied record set. It does not define market identity, grouping identity, snapshot identity, or market price.

## Current Observation Construction

The current code path is:

    AFM response
        -> MarketObservationInput
        -> MarketObservation
        -> MarketRecord[]

The adapter maps the three supported market response types to MarketObservationInput:

- Offers
- Requests
- LoadoutOffers

Each response is converted into one MarketObservation candidate by MarketObservationFactory through MarketProcessing.

The current MarketObservation contains:

- ObservationId
- ResponseType
- ResponseKind
- OperationCode
- CapturedAt
- Records

The records are canonical Reforge MarketRecord values, not AFM DTOs.

## Observation Scope Dimensions

MarketObservationScope currently describes distinct values for:

- ItemTypeId
- LocationId
- QualityLevel
- EnchantmentLevel
- AuctionType

It exposes both the distinct values and HasMultipleX flags.

The implementation does not add ItemGroupTypeId to MarketGroupingKey and does not change the Step 15 grouping rule.

### ResponseKind

ResponseKind remains Observation-level metadata:

- Offers
- Requests
- LoadoutOffers

The code confirms that these are separate response categories.

The code does not establish the market interpretation:

    Offers = sell market snapshot
    Requests = buy market snapshot

That interpretation remains UNVERIFIED.

ResponseKind and MarketRecord.AuctionType remain separate concepts.

### Item scope

The adapter maps all market orders present in the response into the same MarketObservationInput. The canonical MarketRecord retains ItemTypeId per record.

The current model therefore does not guarantee one ItemTypeId per Observation.

The scope calculator can describe multiple ItemTypeId values.

ObservationId is not ItemTypeId.

### Location scope

LocationId exists on each MarketRecord.

There is no Observation.LocationId field.

Therefore the current model does not guarantee one location per Observation. Scope describes the distinct LocationId values present in the records.

### Quality / Enchantment scope

QualityLevel and EnchantmentLevel remain record-level values.

There is no Observation-level QualityLevel or EnchantmentLevel field.

The implementation therefore does not assume all records in an Observation share one quality or enchantment.

### AuctionType scope

AuctionType remains a MarketRecord dimension.

The scope calculator can describe multiple AuctionType values in one supplied record set.

The code does not establish that AuctionType is equivalent to ResponseKind.

## CapturedAt

The current adapter maps CapturedAt from the response object when available.

The existing AFM Core path previously established that CapturedAt originates from captured packet context and propagates through decoded operation context.

Confirmed:

    CapturedAt propagates as captured packet context.

Unverified:

    CapturedAt is the exact moment of server-side market state observation.

Step 16 does not introduce ObservedAt and does not create time buckets.

## Observation Invariants

The current code supports these invariants:

### Observation identity

ObservationId is generated independently of MarketRecord.OrderId.

Duplicate OrderId values do not become Observation identity.

### Record membership

The current MarketObservation model accepts zero or more records.

Existing Step 12 coverage confirms an empty synthetic input can produce an Observation with zero records.

### Record preservation

MarketObservationFactory copies the supplied canonical record collection into the Observation candidate without deduplication or merge logic.

Step 16 scope calculation also does not deduplicate records. It only calculates distinct dimension values for descriptive metadata.

### Scope neutrality

The current Observation model does not declare that an Observation is:

- one item
- one location
- one quality
- one enchantment
- one AuctionType

Those are record-level dimensions.

## MarketObservationScope

Implemented:

    MarketObservationScope(
        IReadOnlyList<string> ItemTypeIds,
        IReadOnlyList<string> LocationIds,
        IReadOnlyList<byte> QualityLevels,
        IReadOnlyList<byte> EnchantmentLevels,
        IReadOnlyList<MarketOrderType> AuctionTypes)

The lists contain distinct values in first-seen order.

The HasMultipleX properties are derived from the corresponding value count.

This is descriptive metadata. It does not mutate the input records.

## Scope vs Grouping

These are intentionally separate:

    MarketObservationScope
        -> What dimension values exist in this Observation/record set?

    MarketGroupingKey
        -> Which records are comparable under the current candidate grouping rule?

Example:

    Observation
      T6 item / Caerleon / Q1 / E0 / Offer
      T6 item / Caerleon / Q2 / E0 / Offer
      T6 item / Caerleon / Q1 / E1 / Offer

The Scope can report:

    ItemTypes: one value
    Locations: one value
    Qualities: two values
    Enchantments: two values

while Step 15 grouping can still produce multiple groups.

Therefore:

    Scope != Grouping

The scope calculator does not call MarketRecordGrouping and does not construct MarketGroupingKey.

## ItemGroupTypeId

ItemGroupTypeId remains a canonical MarketRecord field.

It is not part of the Step 15 MarketGroupingKey.

Step 16 does not add it to the grouping key.

The scope model also does not currently expose ItemGroupTypeId because the required scope dimensions are the current specific-item, location, quality, enchantment, and auction-type dimensions.

Whether ItemGroupTypeId deserves a separate descriptive scope dimension remains an explicit future question rather than an implicit grouping change.

## Why Observation Is Not Yet Snapshot

No Snapshot model was added.

No ObservationId -> SnapshotId rename was made.

No Snapshot table was added.

No grouping-plus-observation rule was promoted to snapshot semantics.

The strongest supported statement remains:

    Observation is a container of records produced by the current AFM response adaptation pipeline.

Whether that container represents a complete market snapshot is UNVERIFIED.

## Tests

MarketObservationScopeTests covers:

- empty collection
- single record
- multiple ItemTypeId values
- multiple LocationId values
- multiple QualityLevel values
- multiple EnchantmentLevel values
- multiple AuctionType values
- repeated identical dimension values
- duplicate OrderId preservation
- independence from MarketGroupingKey
- input immutability
- ItemGroupTypeId not becoming a grouping dimension

Existing Step 15 grouping tests and Step 14 metrics tests remain intact.

No existing test was deleted or weakened.

## Application Demo

The application now displays:

    Observation Scope
      Record Count
      ItemTypes
      Locations
      Qualities
      Enchantments
      AuctionTypes
      HasMultipleX flags

followed by the existing:

    Grouping
      Group count
      Group key
      Record count
      Observed Order Metrics

The demo does not use:

- Market Price
- Market Snapshot
- Market History

## Build/Test Status

- Local build: UNAVAILABLE
- Local test: UNAVAILABLE
- CI build: UNVERIFIED
- CI test: UNVERIFIED

The repository environment does not provide an executed local .NET build/test result for this step.

The latest GitHub commit status has no reported status entries and no workflow runs, so CI cannot be represented as PASS.

## Runtime Status

Runtime: UNAVAILABLE.

No live Albion runtime evidence was available for this step.

Therefore the scope behavior demonstrated here is correctness over canonical synthetic MarketRecord values, not confirmation of real runtime market semantics.

## CONFIRMED

- Current MarketObservation construction path uses MarketObservationInput -> MarketObservation -> MarketRecord[].
- ResponseKind is explicitly represented as Offers, Requests, or LoadoutOffers.
- Observation contains ResponseKind, but does not contain Observation-level Item/Location/Quality/Enchantment/AuctionType fields.
- MarketRecord carries ItemTypeId, LocationId, QualityLevel, EnchantmentLevel, and AuctionType per record.
- Empty observations are structurally supported.
- ObservationId is independent of OrderId.
- Scope calculation is a pure in-memory operation over MarketRecord values.
- Scope calculation does not use MarketGroupingKey.
- Scope calculation does not deduplicate or mutate records.
- Step 15 grouping remains unchanged.
- Step 14 ObservedOrderMetrics remains unchanged.
- Step 4-15 history is preserved.

## INFERRED

- A descriptive scope object is an appropriate boundary for reporting dimension multiplicity without imposing market semantics.
- Distinct dimension values are more useful for future inspection than only boolean HasMultiple flags.
- Keeping ResponseKind outside the record-dimension lists avoids conflating response category with AuctionType.

These are Reforge design decisions/inferences, not confirmed Albion market semantics.

## UNVERIFIED

- Whether one AFM market response corresponds to one complete market query result.
- Whether one response is a complete market state.
- Whether Offers, Requests, and LoadoutOffers correspond to specific sell/buy/snapshot semantics.
- Whether CapturedAt is an exact server-side market observation time.
- Whether one Observation should eventually become a Snapshot.
- Whether ObservationId can eventually serve as snapshot identity.
- Whether ItemGroupTypeId should become a descriptive scope dimension.
- Runtime distribution and semantics of real observations.

## Remaining Questions

1. Does a real AFM response ever contain records spanning multiple items, locations, qualities, enchantments, or auction types?
2. What exact user action/request caused each response in runtime?
3. Does a response represent a complete result or only a server response fragment?
4. How are repeated responses related when the same market query is performed again?
5. What runtime evidence is sufficient to define a true Snapshot boundary?
6. Should a future observation scope include ItemGroupTypeId as descriptive metadata without changing MarketGroupingKey?

## Conclusion

Step 16 establishes an explicit descriptive Observation Scope boundary without redefining Observation as Snapshot.

The implemented separation is:

    Observation Scope
        != Market Identity

    Observation
        != Market Snapshot

    Observation Scope
        != Grouping Key

    Grouping
        != Market Analysis

    MarketRecord
        != Confirmed Market State

The next semantic boundary should be driven by runtime evidence rather than by assuming that an AFM response is already a complete market snapshot.
