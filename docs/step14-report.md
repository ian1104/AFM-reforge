# AFM Reforge — Step 14 Report

## 1. Metric model

New Reforge-owned type: `ObservedOrderMetrics`.

Fields:

- `Count: int`
- `MinUnitPriceSilver: ulong?`
- `MaxUnitPriceSilver: ulong?`
- `AverageUnitPriceSilver: decimal?`
- `TotalAmount: ulong`
- `TotalNotionalSilver: decimal`

The result describes only the supplied canonical records.

## 2. Definitions

### Count
Number of records in the supplied collection.

### Minimum / Maximum Unit Price
Minimum and maximum `MarketRecord.UnitPriceSilver` among supplied records.

### Average Unit Price
Simple arithmetic mean of `UnitPriceSilver`.

No volume/time/market weighting is applied.

### Total Amount
Checked sum of `Amount`.

### Total Notional
Sum of per-record:

`UnitPriceSilver × Amount`

This is stored-order notional, not market value.

## 3. Numeric types

Current canonical source confirms:

- `UnitPriceSilver: ulong`
- `Amount: uint`
- `DistanceFee: ulong`
- `QualityLevel: byte`
- `EnchantmentLevel: byte`

The calculator uses:

- `ulong` for Count-independent total amount.
- `decimal` for average unit price.
- `decimal` for total notional.

This preserves fractional averages and provides a wider practical range for notional arithmetic than `ulong` multiplication alone.

## 4. Overflow policy

The implementation uses explicit checked arithmetic.

- `TotalAmount` uses `checked(ulong + uint)`.
- Total notional is calculated as `decimal(UnitPriceSilver) × Amount`, then accumulated in decimal.
- Decimal overflow is not suppressed.

Therefore arithmetic overflow is surfaced as an exception rather than silently wrapping.

The overflow test uses maximum unsigned values and verifies that the policy is enforced.

## 5. Empty collection policy

For an empty collection:

- Count = 0
- TotalAmount = 0
- TotalNotionalSilver = 0
- MinUnitPriceSilver = null
- MaxUnitPriceSilver = null
- AverageUnitPriceSilver = null

Undefined statistics are therefore not represented as zero.

## 6. Average precision

Average is calculated using `decimal`.

Example:

`100 + 101 -> 100.5`

No integer division occurs.

## 7. Calculator boundary

The calculator is:

`MarketRecord[] -> ObservedOrderMetrics`

It has no AFM Core, SQLite, Query, or runtime dependency.

It does not inspect:

- CapturedAt
- ObservationId
- StorageRecordId
- ResponseKind
- Location grouping
- Item grouping

The caller controls which records belong to the input collection.

## 8. Grouping policy

No automatic grouping is performed.

The calculator treats the supplied collection as one calculation set.

No grouping by:

- ItemTypeId
- LocationId
- QualityLevel
- EnchantmentLevel
- ResponseKind
- ObservationId

is performed.

## 9. AuctionType policy

The calculator does not separate or merge records by `AuctionType`.

Offers and Requests supplied in one collection are both calculation inputs.

If a caller wants separate metrics, the caller must construct separate collections.

## 10. Duplicate OrderId policy

Duplicate `OrderId` values are not deduplicated.

Each canonical MarketRecord contributes independently to:

- Count
- Min
- Max
- Average
- TotalAmount
- TotalNotional

## 11. Tests

Implemented:

- Count
- Minimum
- Maximum
- Decimal average
- Total Amount
- Total Notional
- Empty collection
- Overflow policy
- AuctionType isolation
- Duplicate OrderId preservation

Existing persistence/integration regression coverage was retained and aligned with canonical `MarketRecord`.

## 12. Persistence regression

The existing Step 13 round-trip remains represented by:

```
AFM response
  -> canonical MarketRecord
  -> SQLite
  -> MarketRecordView
```

The Step 14 changes do not modify the database schema or reset existing data.

## 13. Application demo

`AFMReforge.App` creates three synthetic `MOCK_*` records, feeds them through the canonical adapter/persistence pipeline, then calculates:

- Count
- Min Unit Price
- Max Unit Price
- Average Unit Price
- Total Amount
- Total Notional

The output deliberately uses "Observed Order Metrics" terminology and does not call these values market price, market average, or current market value.

## 14. Runtime boundary

Runtime remains unavailable.

Consequently these metrics only describe the records supplied to the calculator.

They do not establish:

- current market price
- market-wide average
- market-wide minimum/maximum
- complete market volume
- market history
- price trend

## 15. Verification

| Area | Status |
|---|---|
| Local build | UNAVAILABLE |
| Local test | UNAVAILABLE |
| CI build | UNVERIFIED |
| CI test | UNVERIFIED |
| Runtime | UNAVAILABLE |

No local PASS is claimed because the local .NET SDK is unavailable.

The GitHub status endpoint for the final head returned no statuses and no workflow run was available for verification, so CI remains UNVERIFIED.

## 16. CONFIRMED

- Current canonical MarketRecord types were rechecked before implementation.
- ObservedOrderMetrics is Reforge-owned.
- Calculator is deterministic and storage-independent.
- Empty metrics distinguish undefined statistics from zero totals.
- Average preserves fractional precision.
- Duplicate OrderId values remain independent inputs.
- AuctionType does not cause implicit grouping.
- No database schema/destructive migration was introduced.
- Step 4~13 history remains preserved.

## 17. INFERRED

- `decimal` is sufficient and simpler than BigInteger for the current notional calculation contract.
- A checked total amount is preferable to silent unsigned wraparound.
- Raw arithmetic metrics are a stable prerequisite for later analysis.

## 18. UNVERIFIED

- Runtime completeness of the supplied order collection.
- Market-wide meaning of any calculated statistic.
- Final market-history semantics.
- Time-series semantics.
- Whether future AFM numeric/type variants require mapper changes.

## 19. Next-step candidates

Potential future work, subject to later instructions:

1. Define explicit metric grouping/query boundaries.
2. Investigate runtime-derived completeness and observation semantics.
3. Design time-aware history only after runtime evidence supports it.
4. Add price-history concepts separately from these raw observed-order metrics.

No Market Price, Market History, Trend, ROI, Arbitrage, or Recommendation logic was introduced.
