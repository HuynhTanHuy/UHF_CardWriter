# Infrastructure Architecture Review — Phase 5

**Date:** 2026-08-06  
**Type:** Clean Architecture readiness review + Infrastructure completion  
**Docs:** [InfrastructureContract.md](InfrastructureContract.md), [InfrastructureDependencyMap.md](InfrastructureDependencyMap.md), [InfrastructureLifetime.md](InfrastructureLifetime.md), [InfrastructureTestPlan.md](InfrastructureTestPlan.md), [ApplicationBoundaryReview.md](ApplicationBoundaryReview.md) (Phase 5A)

---

## Verdict

Infrastructure Layer is **operational and DIP-correct at the assembly boundary**:

- Application has **no** Sdk project reference  
- Infrastructure is the **only** layer that references Sdk  
- Adapters call Sdk only; map to `DeviceResult` + `DeviceErrorCode`  
- Sdk / Driver / Native **not modified**

**Semantic note:** Application ports/DTOs are still **SDK-shaped** (`IUhf*`, `MemBank`, `TagIdentity`). They are **not** renamed in this phase (`Không tự đổi` — change deferred until Use Case services prove the Card* surface). See Technical Debt I-TD-05.

---

## 1. Convention Audit (summary)

| Area | Finding |
|------|---------|
| Solution | App → Application + Infrastructure; Infrastructure → Application + Sdk |
| Application | Ports + Devices DTOs; no services yet |
| Infrastructure | `Devices/*` adapters + DI extension |
| SDK | Wrapper public; Driver public (convention: Infra uses Wrapper only) |
| DI | Singleton session via `AddUhfInfrastructure` |
| Naming clash | App `IUhf*` vs Sdk `IUhf*` — Infra uses aliases |

---

## 2. Application port review (SDK Port vs Use Case Port)

| Current port | Classification | Proposed Use Case port (future) |
|--------------|----------------|----------------------------------|
| `IUhfConnection` | **SDK-shaped port** | `ICardConnection` |
| `IUhfInventory` | **SDK-shaped port** | `ICardScanner` |
| `IUhfWriter` | **SDK-shaped port** | `ICardWriter` |
| `IUhfReader` | **SDK-shaped port** | `ICardReader` |
| `IUhfTagControl` | **SDK-shaped port** | fold into writer/scanner or `ICardTagControl` |
| *(missing)* | — | `ICardDevice` façade (optional) |

**Why not renamed now**

- No Application Service yet to define real Use Case needs  
- Renaming without consumers = churn without proven benefit  
- Assembly isolation already prevents Sdk leakage  

**When to rename:** introduce with WriteCardService / ConnectionService (next Application phase).

---

## 3. DTO review

| Current Application type | Fits Use Case? | Target (future) |
|--------------------------|----------------|-----------------|
| `TagIdentity` | SDK inventory mirror | `CardIdentity` |
| `TagAccessResponse` | SDK TagResp mirror | `CardWriteResult` (or access result) |
| `TagReadData` | SDK read mirror | `CardReadResult` / `CardData` |
| `MemBank` | Gen2 SDK enum | Keep as domain Gen2 concept **or** hide behind write EPC API |
| `DeviceResult` / `DeviceErrorCode` | **Use-case friendly** | Keep |
| `DeviceConstants` | timeouts | Keep / move to options later |

**Native leakage:** Application DTOs do **not** contain `NativeTag*` / `NativeResult` / `STAT_*`.  
`StatusCode` removed from `DeviceResult` in favor of `DeviceErrorCode`.

---

## 4. Device error review

| `DeviceErrorCode` | Typical vendor sources (Infra only) |
|-------------------|-------------------------------------|
| `None` | STAT_OK |
| `ReaderNotConnected` | PORT_HANDLE / UNCONNECT |
| `ReaderOpenFailed` | PORT_OPEN_FAILED |
| `ReaderBusy` | CMD_INNER_ERR |
| `ReaderTimeout` | COMM_TIMEOUT / RESP_FORMAT |
| `TagNotFound` | TAG_NO_RESP / INVENTORY_STOP / NOMORE_DATA |
| `WriteFailed` | COMM_WR_FAILED (+ Unknown on other write fails) |
| `ReadFailed` | COMM_RD_FAILED |
| `InvalidPassword` | PWD_ERR / AUTH_FAIL |
| `TagAccessDenied` | MEM_LCK / OPR_LIMIT |
| `InvalidParameter` | CMD_PARAM_ERR |
| `SdkUnavailable` | DLL_INNER_FAILED |
| `ReaderDisconnected` | DLL_DISCONNECT |
| `Unknown` | unmapped |

---

## 5. Adapter review (short)

| Adapter | Purpose | Output | Exception |
|---------|---------|--------|-----------|
| Connection | Open/close/USB | `DeviceResult` | `DeviceException` / Argument* |
| Inventory | Start/Stop/GetCurrentTag | `DeviceResult` / `TagIdentity` | same |
| Writer | Write | `TagAccessResponse` | same |
| Reader | Read | `TagReadData` | same |
| TagControl | Select/Lock/Kill | `DeviceResult` | same |

All: **not thread-safe**; Singleton; no retry/logging/verify.

---

## 6. Boundary / coupling / extensibility

| Topic | Assessment |
|-------|------------|
| Boundary | Strong at project-reference level |
| Coupling | Adapters ↔ `IUhfSdk` only; mapping centralized |
| Extensibility | Fake adapters easy; Card* ports later; keyed multi-reader later |
| Risk | Teams may treat `IUhf*` as “the domain” — mitigate via docs + future rename |

---

## 7. Technical debt

| ID | Item | Impact | Recommendation | Blocker for Infra? |
|----|------|--------|----------------|--------------------|
| I-TD-01 | Duplicate `IUhf*` names App vs Sdk | Low | Aliases in Infra | No |
| I-TD-02 | SDK-shaped DTOs (`TagIdentity`, `MemBank`) | Medium for CA purity | Rename with Use Case services | No |
| I-TD-03 | SDK-shaped ports (`IUhf*`) | Medium | Introduce `ICard*` when services land | No |
| I-TD-04 | Host not calling `AddUhfInfrastructure` | Low | Wire at composition root with UI/services | No |
| I-TD-05 | No automated Infra tests yet | Medium | Follow TestPlan | No |
| I-TD-06 | `DeviceErrorMapper` duplicates STAT literals | Low | Keep until Sdk exposes public status map (do not change Sdk now) | No |

---

## 8. SDK / Driver / Native change requests

| Request | Action |
|---------|--------|
| Public status→error map on Sdk | **Not applied** — Infra owns map |
| Make Driver internal | **Not applied** |
| RF Power | Still deferred (no Driver API) |

---

## 9. Recommendation

1. Keep current adapters for Infrastructure Phase 5 gate.  
2. Next Application phase: introduce Use Case ports (`ICard*`) + DTOs (`CardIdentity`, …) and thin adapters or rename ports with a single breaking pass.  
3. Always consume `DeviceErrorCode`, never vendor ints.  
4. Serialize all device calls on one worker.

---

## Checklist (this review)

- [x] Infrastructure sole Sdk consumer  
- [x] Application no Sdk assembly reference  
- [x] `DeviceErrorCode` present + mapped  
- [x] DTO without Native types  
- [x] Adapters call Sdk only  
- [x] Driver / Native / Sdk Wrapper unmodified  
- [x] Contract / Lifetime / TestPlan / DependencyMap  
- [ ] Application ports fully Use Case shaped — **deferred** (I-TD-03, documented)
