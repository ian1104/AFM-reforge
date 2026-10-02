# AFM Reforge

Step 5 establishes the minimum Solution and AFM Adapter boundary.

```text
AFMDataClientCore
        ↓ typed Market response
AFMReforge.Adapter.AFM
        ↓ Reforge-owned representation
AFMReforge.Core
        ↓
Future layers
```

The Step 4 runtime probe documentation remains under `docs/runtime-probe/`.

SQLite, UI, Market Assistant, and final MarketObservation semantics are intentionally not implemented.
