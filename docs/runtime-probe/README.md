# AFM Reforge — Runtime Market Observation Probe

## Status

Step 4 probe infrastructure is prepared, but actual Albion runtime verification is currently unavailable.

- Repository: `ian1104/AFM-reforge`
- AFM Core: `e56a049be7c012ea37aa86367156007f64b148d1`
- Actual Albion client runtime: UNAVAILABLE in current execution environment
- Live packet capture: UNAVAILABLE
- Runtime Market response evidence: UNVERIFIED

## Purpose

This probe must observe the existing AFM typed Market response subscriptions without modifying AFM Core or replacing the existing AFM upload path.

Target flow:

```
Albion
  ↓
AFMDataClientCore
  ↓
Typed Market Response
  ├── Existing AFM Handler
  └── Reforge Probe Handler
          ↓
      Console / Log
```

The probe must not introduce SQLite, UI, Market Assistant logic, AODP integration, global market scanning, ROI analysis, or AFM Core modifications.

## Required response subscriptions

- `AuctionGetOffersResponse`
- `AuctionGetRequestsResponse`
- `AuctionGetLoadoutOffersResponse`

For each response record:

### Response level

- UTC timestamp
- response type
- operation code
- ConnectionId
- CapturedAt

### MarketOrder level

- Id
- ItemTypeId
- ItemGroupTypeId
- LocationId
- Resolved Location
- QualityLevel
- EnchantmentLevel
- UnitPriceSilver
- Amount
- AuctionType
- Expires
- DistanceFee

## Runtime scenarios

Run in this order when Albion runtime becomes available:

1. Market open
2. Specific item query
3. Item A → Item B
4. Same item re-query
5. Same item in another city
6. Search/sort/quality/enchantment/tab interactions that actually exist in the client

For each scenario preserve raw probe logs.

## Required conclusions

Do not decide the final Observation domain unit before runtime evidence exists.

Classify every conclusion as:

- RUNTIME CONFIRMED
- MOCK CONFIRMED
- INFERRED
- UNVERIFIED
- PROPOSAL

## Important restriction

Never report static or mock behavior as actual Albion runtime behavior.
Never modify AFMDataClientCore to make the probe work.
Never intentionally break the existing AFM upload path in live runtime.
