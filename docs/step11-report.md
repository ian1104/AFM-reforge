# Step 11 Report — Runtime Observation Candidate Validation

## 1. Runtime environment

Required runtime components:

- AFM Reforge execution environment
- AFMDataClientCore
- Albion Online client
- packet capture / AFM capture path
- real market interaction

The current execution environment does not provide an Albion client or a live AFM capture session. Repository inspection also found no separate runtime host in AFM Reforge that could substitute for the actual Albion client.

The existing Reforge application is a synthetic/mock console host. Its current output path explicitly labels runtime as unavailable.

## 2. Execution availability

**Runtime: UNAVAILABLE**

No real Albion packet or Market response was received during Step 11.

This is an environment limitation, not a runtime result.

No synthetic response was promoted to runtime evidence.

## 3. Test A–J results

| Test | Result | Status |
|---|---|---|
| A — Single market interaction | No live interaction possible | RUNTIME UNAVAILABLE |
| B — Offers | No live Offers response | RUNTIME UNAVAILABLE |
| C — Requests | No live Requests response | RUNTIME UNAVAILABLE |
| D — LoadoutOffers | No live LoadoutOffers response | RUNTIME UNAVAILABLE |
| E — Repeated interaction | No live repeated responses | RUNTIME UNAVAILABLE |
| F — Multiple items | No live item queries | RUNTIME UNAVAILABLE |
| G — Location | No live cross-city queries | RUNTIME UNAVAILABLE |
| H — CapturedAt | No live timestamps available | RUNTIME UNAVAILABLE |
| I — AFM coexistence | No live upload session | UNVERIFIED |
| J — Duplicate behavior | No live duplicate observation | RUNTIME UNAVAILABLE |

Detailed evidence is recorded in `docs/runtime-probe/step11-observations.md`.

## 4. Response sequence

No runtime response sequence was captured.

Therefore the following remain unknown:

- number of responses per UI interaction
- response arrival order
- response sequence identifiers
- ordering among Offers, Requests, and LoadoutOffers
- candidate creation order relative to actual response arrival

No sequence was fabricated.

## 5. Offers result

The Reforge adapter statically subscribes to `AuctionGetOffersResponse` and maps its market orders into `MarketObservationInput`.

No actual Offers response was observed.

Unknown:

- order count
- ItemTypeId distribution
- LocationId distribution
- QualityLevel distribution
- EnchantmentLevel distribution
- AuctionType distribution
- MarketOrder.Id distribution
- whether response-level uniformity exists

**Status: RUNTIME UNAVAILABLE**

## 6. Requests result

The adapter statically subscribes to `AuctionGetRequestsResponse`.

No actual Requests response was observed.

Whether Offers and Requests occur together for a single user interaction remains unknown.

**Status: RUNTIME UNAVAILABLE**

## 7. Loadout result

The adapter statically subscribes to `AuctionGetLoadoutOffersResponse`.

No actual LoadoutOffers response was observed.

This step therefore does not conclude that the response is absent from the real client. It is simply not observed.

**Status: RUNTIME UNAVAILABLE**

## 8. Item / Location result

No runtime item or city comparison was possible.

Consequently, the following cannot yet be promoted to Observation-level invariants:

- ItemTypeId
- LocationId
- QualityLevel
- EnchantmentLevel

The current implementation correctly retains these values at MarketRecord level without introducing Observation-level copies.

**Status: UNVERIFIED**

## 9. Duplicate result

No runtime duplicate behavior was observed.

The existing implementation does not deduplicate MarketOrder.Id and keeps StorageRecordId independent from both ObservationId and MarketOrder.Id.

That is an implementation behavior, not a statement about how Albion repeats orders in runtime.

**Status: STATIC CONFIRMED / RUNTIME UNAVAILABLE**

## 10. CapturedAt result

Static inspection confirms that the Reforge adapter reads and propagates the response `CapturedAt` into `MarketObservationInput`, and the existing candidate carries that value.

No live packet/handler/persistence timing comparison was possible.

Therefore:

- `CapturedAt` propagation: **STATIC CONFIRMED**
- `CapturedAt` as packet observation time in actual runtime: **UNVERIFIED**
- `CapturedAt` as Albion server market-state timestamp: **UNVERIFIED**

## 11. AFM coexistence result

The adapter's live configuration path preserves the existing AFM registration call and then registers Reforge's typed market subscriptions.

No AFM Core source was modified.

However, no live Albion session was available to verify:

- existing AFM upload success
- Reforge persistence success under live traffic
- coexistence under actual handler execution
- runtime exception isolation

Therefore the coexistence result is:

**UNVERIFIED**

## 12. SQLite persistence result

No runtime-generated SQLite candidate was available.

The existing Step 10 implementation provides the following static persistence behavior:

- one candidate can be persisted with its associated records
- associated records receive the candidate ObservationId
- StorageRecordId remains local storage identity
- duplicate MarketOrder.Id values remain separate records
- CapturedAt and ResponseKind are retained
- legacy records may retain NULL ObservationId

No destructive database operation was performed in Step 11.

**Status: STATIC CONFIRMED / RUNTIME UNAVAILABLE**

## 13. Current Observation candidate validation

Current candidate construction remains:

`1 MarketObservationInput → 1 MarketObservation candidate`

Step 11 did not obtain the runtime evidence required to decide whether that boundary represents:

- one actual market response
- one complete market observation
- one UI interaction
- one Snapshot
- or only a transport-level response candidate

Therefore the current candidate model is **not promoted to confirmed Albion market semantics**.

No Snapshot implementation was added.

## 14. CONFIRMED

### Static implementation

- Reforge adapter has typed subscriptions for Offers, Requests, and LoadoutOffers.
- Existing AFM handler registration is retained.
- Market response fields are mapped into Reforge-owned input objects.
- CapturedAt is propagated through the current static Reforge pipeline.
- ObservationId, StorageRecordId, and MarketOrder.Id remain distinct identities.
- Step 10 persistence associates candidate-generated records through ObservationId.
- Duplicate Order IDs are not automatically deduplicated.
- No AFMDataClientCore source was changed in Step 11.
- No Snapshot, MarketHistory, PriceHistory, Trend, Spread, ROI, recommendation, or automatic deduplication feature was added.

### Runtime

No runtime semantics were confirmed.

## 15. INFERRED

The existing adapter is structurally prepared to observe the required response types without modifying AFM Core.

This does not establish how Albion actually emits those responses.

The current one-candidate-to-many-record persistence relationship remains a Reforge implementation choice rather than a confirmed Albion market semantic.

## 16. UNVERIFIED

- One UI interaction → response count
- One response → final Observation
- One response → Snapshot
- Response completeness
- Response grouping across packets
- Offers/Requests/LoadoutOffers higher-level grouping
- Response-wide ItemType/Location/Quality/Enchantment invariants
- Repeated OrderId runtime behavior
- CapturedAt semantic meaning in runtime
- AFM upload coexistence under live traffic
- Whether Candidate A remains sufficient after runtime evidence
- Final market-history semantics

## 17. Next steps

The next runtime-capable step should resume the probe rather than add analysis features.

Priority order:

1. Obtain an environment containing the actual Albion client and existing AFM capture path.
2. Execute Test A and preserve raw response logs.
3. Execute Offers / Requests / LoadoutOffers probes.
4. Repeat identical queries and compare OrderId/CapturedAt/order counts.
5. Compare different items and locations.
6. Verify live AFM upload + Reforge persistence coexistence.
7. Inspect runtime-generated SQLite relationships.
8. Only then decide whether the current candidate boundary needs revision or a higher-level Snapshot concept.

No final Snapshot or MarketHistory semantics should be inferred from this Step 11 run.

## 18. Verification status

Source implementation: **COMPLETED / STATIC INSPECTION**

Local build: **UNAVAILABLE**

Local test: **UNAVAILABLE**

CI build: **UNVERIFIED** — current GitHub status query returned no status checks and no workflow runs for Step 10 HEAD.

CI test: **UNVERIFIED** — same limitation.

Runtime: **UNAVAILABLE**

Runtime-confirmed semantics: **NONE**

Still unverified: **All Albion runtime-dependent market-response semantics listed above.**

## 19. Git safety

Step 4–10 history is preserved.

No destructive Git operation was used.

No AFM Core source was modified.

No destructive database migration was performed.

## 20. Core principle

`Static code ≠ Runtime behavior`

`Mock behavior ≠ Albion runtime behavior`

`Observation candidate ≠ Confirmed Market Observation`

Step 11 therefore records an explicit runtime-unavailable result rather than manufacturing market-response evidence.
