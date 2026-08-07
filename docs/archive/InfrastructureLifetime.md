# Infrastructure DI Lifetime

**Phase:** 5  
**Registration:** `AddUhfInfrastructure(IServiceCollection)`  
**Related:** [InfrastructureContract.md](InfrastructureContract.md), ADR-004 (Driver thread safety)

---

## Registered services

| Service | Implementation | Lifetime |
|---------|----------------|----------|
| `IUhfSdk` | `UhfPrimeSdk` | **Singleton** |
| `IUhfConnection` (Application) | `UhfConnectionAdapter` | **Singleton** |
| `IUhfInventory` (Application) | `UhfInventoryAdapter` | **Singleton** |
| `IUhfWriter` (Application) | `UhfWriterAdapter` | **Singleton** |
| `IUhfReader` (Application) | `UhfReaderAdapter` | **Singleton** |
| `IUhfTagControl` (Application) | `UhfTagControlAdapter` | **Singleton** |

---

## Why Singleton

1. **One native handle** — Driver/SDK own a single connection; multiple SDK instances fighting one reader is undefined.
2. **WinForms host** — typically one process-wide composition root, not per-HTTP-request scopes.
3. **Adapters are stateless** — they only hold `IUhfSdk`; no per-call mutable state.
4. **Matches SDK readiness** — one SDK ↔ one Driver ↔ one reader session.

---

## Why not Scoped

| Reason | Detail |
|--------|--------|
| No ambient scope | Classic WinForms has no request scope unless a custom scope is invented |
| Accidental multi-handle | New scope → new SDK if registered Scoped → second Open conflict |
| Session length | Reader session outlives a single UI action |

Use an explicit **named scope / factory** later if the product needs multi-reader (see below).

---

## Why not Transient

| Reason | Detail |
|--------|--------|
| New `UhfPrimeSdk` per resolve | Multiple Drivers / handles |
| Dispose chaos | Who owns Dispose of each transient SDK? |
| Adapters would disagree | Connection adapter’s SDK ≠ Writer adapter’s SDK |

---

## Dispose

- `UhfPrimeSdk` implements `IDisposable`.
- MS.DI disposes singleton `IUhfSdk` when the root `ServiceProvider` is disposed.
- Prefer explicit `Connection.Close()` when status matters; Dispose is best-effort.

---

## Multiple readers (future)

Current registration assumes **one reader per process**.

| Approach | Notes |
|----------|-------|
| A. Keyed DI (`AddKeyedSingleton`) | One `IUhfSdk` per reader key (reader id / COM / IP) |
| B. `IUhfSdkFactory.Create(connectionOptions)` | Transient/scoped SDK per session; adapters become session-bound |
| C. Multiple hosts | Separate process per reader |

**Do not** switch to Transient SDK without a factory design. Document a new ADR when multi-reader is required.

---

## Threading vs lifetime

Singleton ≠ thread-safe. Lifetime only ensures **one instance**; callers must still serialize calls (worker thread / lock above Infrastructure).
