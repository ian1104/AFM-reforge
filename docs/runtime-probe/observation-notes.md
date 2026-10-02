# Step 4 Observation Notes

## Environment

| Item | Value | Status |
|---|---|---|
| OS | Current development execution environment | CONFIRMED |
| .NET | Not yet established for live probe | UNVERIFIED |
| AFM Core commit | e56a049be7c012ea37aa86367156007f64b148d1 | STATIC CONFIRMED |
| Albion client version | Not available | UNVERIFIED |
| Albion executable/runtime | Not available | RUNTIME UNAVAILABLE |
| Live UDP capture | Not available | RUNTIME UNAVAILABLE |

## Test Scenarios

| Test | Action | Result |
|---|---|---|
| A | Market open | UNVERIFIED |
| B | Specific item query | UNVERIFIED |
| C | Item change | UNVERIFIED |
| D | Same item re-query | UNVERIFIED |
| E | City change | UNVERIFIED |
| F | UI exploration | UNVERIFIED |

## Actual Response Patterns

No live Albion responses were observed in this execution.

Therefore the following remain UNVERIFIED:

- AuctionGetOffersResponse occurrence
- AuctionGetRequestsResponse occurrence
- AuctionGetLoadoutOffersResponse occurrence
- response frequency
- orders per response
- duplicate order behavior
- automatic/background market requests

## CapturedAt

Static source confirms the propagation path:

CapturedDatagram.CapturedAt → DecodedPacket.CapturedAt → BaseOperation.CapturedAt

Actual runtime semantics and precision are UNVERIFIED.

## Duplicate Analysis

No live MarketOrder IDs were observed.

No deduplication policy is being introduced in Step 4.

## Existing AFM Upload Compatibility

| Item | Result |
|---|---|
| Existing AFM handler | STATIC CONFIRMED |
| Reforge typed subscription architecture | STATIC CONFIRMED |
| Existing MarketUpload path | STATIC CONFIRMED |
| Reforge observation in live runtime | UNVERIFIED |
| Exception isolation in live runtime | UNVERIFIED |
| Runtime handler ordering | UNVERIFIED |

## Observation Semantics

No final Observation unit is selected.

Response / MarketOrder / UI Query / Snapshot semantics remain UNVERIFIED pending live evidence.

## Step 4 Final Status

**STATIC CONFIRMED**

- Multiple typed subscribers are supported by AFM Core.
- Existing AFM upload can coexist architecturally with a Reforge subscriber.
- Typed Market response DTOs are available to the Reforge boundary.
- CapturedAt is propagated into BaseOperation.

**RUNTIME UNAVAILABLE**

The current execution environment cannot launch Albion Online or capture its real market packets. Consequently no runtime claims are made.

## Next Runtime Evidence Required

Run the prepared probe on the actual Albion client and preserve the resulting log. Then update this document with the observed response sequence, order counts, timestamps, duplicate IDs, city changes, and coexistence behavior.

Do not design the final SQLite schema or MarketObservation domain until those observations are available.
