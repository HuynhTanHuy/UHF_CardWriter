# SDK Wrapper Architecture Review — Phase 4

**Date:** 2026-08-06  
**Entry:** `IUhfSdk` / `UhfPrimeSdk`  
**Docs:** [SDKWrapperContract.md](SDKWrapperContract.md), [SDKWrapperDependencyMap.md](SDKWrapperDependencyMap.md), [SDKWrapperReadiness.md](SDKWrapperReadiness.md)

---

## Verdict

Phase 4 Wrapper is **complete** for Infrastructure consumption:

- Public API is managed-only (`SdkResult`, `TagIdentity`, `TagReadData`, …)
- Implementation calls **only** `UhfPrimeDriver`
- No business / retry / logging / verify / inventory poll loops
- Driver and Native were **not** modified

---

## Boundary

| Layer | Allowed to call |
|-------|-----------------|
| Infrastructure (Phase 6) | `IUhfSdk` facets only |
| Wrapper | `UhfPrimeDriver` |
| Driver | `UhfPrimeNative` |
| Application / UI | Never Driver/Native; use Application services → Infrastructure → `IUhfSdk` |

**Note:** `UhfPrimeDriver` remains `public` from Phase 3 (not redesigned). Convention for Phase 4+: treat Driver as non-consumer API; use Wrapper. Making Driver `internal` would be a later optional hardening ADR.

---

## Dependency / Layering

```
IUhfSdk
  ├─ IUhfConnection  → Open*/Close/USB
  ├─ IUhfInventory   → Start/Stop/GetCurrentTag (single calls)
  ├─ IUhfWriter      → WriteTag + GetTagResp
  ├─ IUhfReader      → ReadTag + GetReadTagResp
  └─ IUhfTagControl  → Select/Lock/Kill
         ↓
   UhfPrimeDriver
         ↓
   UhfPrimeNative
```

Write/Read composition of command + response is **SDK access pairing**, not Application workflow (no verify, no select, no stop).

---

## Abstraction

| Choice | Rationale |
|--------|-----------|
| Facet interfaces on one class | Simple; one Driver lifetime; easy to mock `IUhfSdk` later |
| `SdkResult` instead of `NativeResult` | Hides Driver types from Infrastructure |
| `MemBank` public enum | Avoids exposing `NativeMemBank` |
| No `IUhfPower` / device para | Driver lacks APIs — deferred (see debt) |

---

## State management

| State | Held? |
|-------|-------|
| Driver handle | Yes (inside Driver) |
| `IsOpen` | Projected from Driver |
| Inventory running flag | **No** — caller tracks if needed |
| Selected EPC | **No** — business/Application |

---

## Thread safety

**Not thread-safe.** One `UhfPrimeSdk` ↔ one Driver ↔ one reader session.  
Caller (Infrastructure worker) must serialize. No locks added (consistent with ADR-004).

---

## Extensibility

- Phase 5 Application interfaces can wrap `IUhfSdk` without leaking Driver.
- Power/Device facets can be added when Driver exposes matching methods (new ADR).

---

## Technical debt / gaps

| ID | Item | Impact | Recommendation | Blocker? |
|----|------|--------|----------------|----------|
| W-TD-01 | Driver still public | Low | Optional future: `internal` Driver + InternalsVisibleTo | No |
| W-TD-02 | No RF Power on Wrapper | Medium for power UI | Add Driver RF methods then `IUhfPower` | No for write-card core |
| W-TD-03 | No DevicePara | Low | Same as W-TD-02 | No |
| W-TD-04 | Write/Read don't call InventoryStop | None | Correct — Application/flow owns stop | No |

---

## Driver / Native change requests

| Request | Action taken |
|---------|--------------|
| Add RF power to Driver for `IUhfPower` | **Not applied** — reported only |
| Make Driver internal | **Not applied** — would be redesign of Phase 3 visibility |

---

## Checklist

- [x] Driver not modified
- [x] Native not modified
- [x] No IntPtr / native struct / NativeResult on Wrapper public API
- [x] Wrapper only calls Driver
- [x] No business / retry / logging / polling loops
- [x] XML docs on public Wrapper types
- [x] Contract + dependency map + this review
