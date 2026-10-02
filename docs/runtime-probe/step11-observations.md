# Step 11 Runtime Probe Observations

## Runtime availability

Environment inspection was performed against the AFM Reforge repository at Step 10 HEAD `7e4dae66b30f3768694a362de25e3b18f3da6c4f`.

The repository contains the Reforge mock application, AFM adapter, Core, Infrastructure, and tests, but no Albion client runtime or live packet-capture host is available in the current execution environment. The existing application is explicitly synthetic/mock-driven and prints `Runtime status: UNAVAILABLE`.

Therefore no real Albion market interaction was executed in this step.

**Status: RUNTIME UNAVAILABLE**

## Instrumentation

No AFMDataClientCore source was modified.

The existing Reforge adapter already exposes typed subscriptions for:

- `AuctionGetOffersResponse`
- `AuctionGetRequestsResponse`
- `AuctionGetLoadoutOffersResponse`

and maps their `CapturedAt`, operation code, and MarketOrder fields into `MarketObservationInput`.

Because the actual Albion runtime and capture path are unavailable, additional runtime-only instrumentation was not added merely to simulate evidence.

**Status: STATIC CONFIRMED**

## Test A — Single market interaction

**Environment:** Albion runtime unavailable.

**Action:** Attempted to establish whether the current environment can execute an actual market interaction through AFM capture.

**Observed:** No Albion client/capture session is available. No real market response was received.

**Interpretation:** A UI interaction cannot be mapped to a response count without runtime evidence.

**Status: RUNTIME UNAVAILABLE**

## Test B — Offers

**Environment:** Albion runtime unavailable.

**Action:** Probe `AuctionGetOffersResponse`.

**Observed:** No live response.

**Interpretation:** Order count, ItemTypeId distribution, LocationId distribution, QualityLevel distribution, EnchantmentLevel distribution, AuctionType distribution, and MarketOrder.Id distribution remain unknown.

**Status: RUNTIME UNAVAILABLE**

## Test C — Requests

**Environment:** Albion runtime unavailable.

**Action:** Probe `AuctionGetRequestsResponse`.

**Observed:** No live response.

**Interpretation:** Whether Requests co-occur with Offers during one user interaction remains unknown.

**Status: RUNTIME UNAVAILABLE**

## Test D — LoadoutOffers

**Environment:** Albion runtime unavailable.

**Action:** Probe `AuctionGetLoadoutOffersResponse`.

**Observed:** No live response.

**Interpretation:** No claim is made about when LoadoutOffers occurs or whether it occurs in the tested client flow.

**Status: RUNTIME UNAVAILABLE**

## Test E — Repeated interaction

**Environment:** Albion runtime unavailable.

**Action:** Repeat an identical market query.

**Observed:** No live responses.

**Interpretation:** Repeated Order IDs, order-count changes, CapturedAt changes, price changes, amount changes, and response sequence behavior remain unknown.

**Status: RUNTIME UNAVAILABLE**

## Test F — Multiple items

**Environment:** Albion runtime unavailable.

**Action:** Query two different ItemTypeIds.

**Observed:** No live responses.

**Interpretation:** Response, item, and candidate boundaries cannot be established from runtime evidence.

**Status: RUNTIME UNAVAILABLE**

## Test G — Location

**Environment:** Albion runtime unavailable.

**Action:** Query the same item in two cities.

**Observed:** No live responses.

**Interpretation:** LocationId cannot be classified as a response-level invariant or merely a MarketOrder-level field from this step.

**Status: RUNTIME UNAVAILABLE**

## Test H — CapturedAt

**Environment:** Albion runtime unavailable.

**Action:** Compare packet capture time, decoded response time, handler processing time, candidate creation time, and SQLite persistence time.

**Observed:** No live packet/response timestamps were available.

**Interpretation:** The existing static pipeline preserves `CapturedAt` from the AFM response boundary, but this does not establish its semantic meaning in Albion runtime.

**Status: RUNTIME UNAVAILABLE**

## Test I — AFM upload coexistence

**Environment:** Albion runtime unavailable.

**Action:** Attempt simultaneous existing AFM upload and Reforge persistence.

**Observed:** No live upload session was available.

**Interpretation:** Coexistence remains unverified at runtime. Static inspection shows the Reforge adapter invokes the existing AFM `RegisterHandlers(builder)` before adding its own typed market subscriptions; no AFM Core source was modified.

**Status: UNVERIFIED**

## Test J — Duplicate behavior

**Environment:** Albion runtime unavailable.

**Action:** Observe duplicate MarketOrder.Id within and across live responses.

**Observed:** No live responses.

**Interpretation:** Runtime duplicate behavior remains unknown. The Reforge persistence implementation continues to preserve duplicate Order IDs rather than deduplicating them.

**Status: RUNTIME UNAVAILABLE**

## SQLite persistence probe

No live runtime candidate was available, so no runtime-generated SQLite rows were produced for inspection.

The implemented storage model is statically confirmed to associate candidate-generated records with the same ObservationId and to preserve StorageRecordId, MarketOrder.Id, CapturedAt, and ResponseKind. These are implementation semantics, not runtime evidence.

**Status: RUNTIME UNAVAILABLE**

## Observation candidate comparison

Current implementation:

`MarketObservationInput → MarketObservation candidate`

was not contradicted or confirmed by live Albion behavior because no runtime response was available.

No Snapshot or cross-response grouping was introduced.

**Status: UNVERIFIED**

## Evidence boundary

The following remain unresolved:

- actual response count per market interaction
- Offers/Requests/LoadoutOffers runtime occurrence
- response completeness
- response-wide vs record-level ItemType/Location/Quality/Enchantment behavior
- repeated Order ID behavior
- response sequence behavior
- CapturedAt runtime meaning
- AFM upload coexistence in a live session
- final Observation semantics
- whether Candidate A remains adequate after runtime evidence
