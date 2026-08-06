# Application Boundary Migration Plan

**Status:** Planned — **no code in Phase 5A**  
**Trigger:** Start of Application Use Case phase (Write Card / Connection / Scan)  
**Related:** [ApplicationBoundaryReview.md](ApplicationBoundaryReview.md)

---

## Goal

Replace SDK-shaped Application ports/DTOs with **CareHR card-domain** names and shapes justified by real Use Cases — without changing Native / Driver / Sdk Wrapper contracts.

Infrastructure adapters will be updated to implement the new ports (still calling `IUhfSdk`).

---

## Target Use Cases (drive the ports)

| Use Case | Proposed Application API (illustrative) |
|----------|----------------------------------------|
| Connect Reader | `ICardConnection.OpenHid/OpenSerial/OpenNet/Close` |
| Scan Card | `ICardScanner.StartScan` / `TryGetCard` / `StopScan` |
| Select Card | `ICardScanner.SelectByEpc` or writer precondition |
| Write Card | `ICardWriter.WriteEpc(current, new, password)` — hide `MemBank`/`wordPtr` defaults |
| Read Card | `ICardReader.ReadEpc` / `ReadUser` |
| Verify Card | Application service using Reader (not a raw SDK mirror) |
| Register Card | `ITagRegistryClient` (HTTP) — separate port |

Exact signatures **must** be finalized when services are designed — this plan only constrains **language and layering**.

---

## Mapping: current → target

| Current (Application) | Target | Infra impact |
|-----------------------|--------|--------------|
| `IUhfConnection` | `ICardConnection` | Rename implementor or new adapter class |
| `IUhfInventory` | `ICardScanner` | Map Start/Stop/GetCurrentTag |
| `IUhfWriter` | `ICardWriter` | Prefer higher-level WriteEpc; adapter fills bank/ptr |
| `IUhfReader` | `ICardReader` | Prefer ReadEpc helpers |
| `IUhfTagControl` | Merge into scanner/writer **or** `ICardSecurity` | Avoid dumping Lock/Kill on UI early |
| `TagIdentity` | `CardIdentity` | `SdkMapping` field copy |
| `TagAccessResponse` | `CardWriteResult` | Map TagStatus/Code |
| `TagReadData` | `CardReadResult` | Map Data/WordCount |
| `MemBank` | Remove from public Write Card API **or** `CardMemoryArea` | Adapter uses Sdk `MemBank` internally |
| `DeviceResult` / `DeviceErrorCode` | **Keep** | None |

Sdk types (`Sdk.IUhfConnection`, etc.) **unchanged**.

---

## File impact (expected)

| Area | Files (approx.) | Risk |
|------|-----------------|------|
| Application | New Abstractionsions + Devices; delete or obsolete `IUhf*` / `Tag*` | Medium — breaking for any early consumers |
| Infrastructure | Adapters + DI registration + `SdkMapping` | Medium |
| App host | Only if already resolving `IUhf*` | Low today (Form1 unused) |
| Docs | Update Infra contract/dependency map | Low |
| Sdk / Driver / Native | **None** | — |

---

## Backward compatibility

| Approach | Notes |
|----------|-------|
| Big-bang rename | Acceptable while no production consumers of Application ports |
| Obsolete aliases | Optional `[Obsolete] IUhfWriter` → `ICardWriter` for one sprint |
| Parallel ports | Avoid long dual surface — increases debt |

**Recommendation:** big-bang at start of Use Case phase while App still only uses placeholder UI.

---

## Dependencies / order

1. Define Use Case service interfaces & DTOs (`WriteCardRequest`, …).  
2. Introduce domain ports (`ICard*`) required by those services.  
3. Rebind Infrastructure adapters + DI.  
4. Remove SDK-named Application ports/DTOs.  
5. Update docs / close I-TD-02 / I-TD-03.

**Do not** rename ports in isolation without step 1–2 (violates “prove by Use Case”).

---

## Risks

| Risk | Mitigation |
|------|------------|
| Over-abstract WriteEpc too early | Start with thin rename; add helpers when WriteCardService needs them |
| Lock/Kill unused | Omit from first Card ports until a Use Case exists |
| Confusion App `IUhf*` vs Sdk `IUhf*` | Migration eliminates App `IUhf*` |

---

## Out of scope for this plan

- Implementing WriteCardService / UI / HTTP  
- Changing Sdk Wrapper public API  
- Multi-reader DI
