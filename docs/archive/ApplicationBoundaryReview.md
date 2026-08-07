# Application Boundary Review — Phase 5A

**Date:** 2026-08-06  
**Type:** Architecture gate (review only — no rename / no refactor)  
**Related:** [ApplicationBoundaryMigrationPlan.md](ApplicationBoundaryMigrationPlan.md), [InfrastructureReview.md](InfrastructureReview.md)

---

## Verdict (Phase 5A)

| Criterion | Result |
|-----------|--------|
| Assembly isolation (Application ↛ Sdk) | **Pass** |
| No Native / STAT_* on Application API | **Pass** (`DeviceErrorCode`) |
| Application uses **Business / Domain language** | **Fail** — ports/DTOs are SDK-shaped |
| Use Cases implemented & justifying ports | **Fail** — no Application Services / Use Cases in source |
| Intentional permanent SDK terminology (ADR) | **Rejected** — product is CareHR card writer, not a shared RFID SDK |

**Phase 5 Final Gate: NOT COMPLETE** — see blockers at end of this document and §10 of the review response.

---

## 0. Convention Audit (source-based)

| Layer | Observation |
|-------|-------------|
| Solution name | `CareHR.UhfCardWriter` — product app, not `UhfPrime.Sdk` host |
| App | WinForms host (`Form1` placeholder); no WriteCard UI yet |
| Application | `Abstractions/IUhf*`, `Devices/*` only; AssemblyInfo: *“Business services are later phases”* |
| Infrastructure | Adapters implement Application `IUhf*`; sole Sdk consumer |
| Sdk | `IUhfSdk`, Driver, Native — RFID vendor vocabulary (correct **here**) |
| Dependency rule | Application has **no** Sdk project reference — **correct** |
| Naming | Application ports **duplicate Sdk facet names** (`IUhfConnection`, …) |

---

## 1. Business Scope Audit

**Conclusion: B — CareHR card-writing business application.**

| Evidence | Location |
|----------|----------|
| Product / solution name | `CareHR.UhfCardWriter` |
| Stated scope | SDK report §13: *“phạm vi app ghi thẻ CareHR”* |
| Architecture intent | Docs: WriteCardForm, WriteCardService, verify, CareHR registry API |
| Reference product | CardWritter pattern (hospital card write) — not a reusable RFID SDK package |
| Sdk project role | **Supporting** library inside the solution, not the product |

**Implication:** Application layer **must** speak CareHR / card domain language. SDK terminology is valid only inside `CareHR.UhfCardWriter.Sdk` (and Infrastructure mapping).

This is **not** A (shared RFID SDK). Therefore permanent `IUhf*` on Application is **not** justified.

---

## 2. Use Case Audit

### Implemented in Application source

**None.** No `*Service`, no `WriteCardRequest`, no use-case handlers under Application.

### Planned / documented Use Cases (architecture docs — not code)

| Use Case | Business intent | Port that would serve (today’s names) | Port justified by implemented Use Case? |
|----------|-----------------|----------------------------------------|----------------------------------------|
| Connect Reader | Open COM/HID/Net | `IUhfConnection` | **No** — no ConnectionService |
| Scan Card | Find tag / EPC in field | `IUhfInventory` (+ Select) | **No** |
| Select Card | Mask by current EPC | `IUhfTagControl.Select` | **No** |
| Write Card | Write new EPC / payload | `IUhfWriter` | **No** |
| Read Card | Read back memory | `IUhfReader` | **No** |
| Verify Card | Compare expected vs read | *(needs Reader + policy — not a raw port alone)* | **No** |
| Register Card | POST CareHR backend | *(ITagRegistryClient — not present)* | **No** |
| Lock / Kill Card | Rare ops | `IUhfTagControl` | **No** |

**Rule applied:** *If no Use Case → Port should not exist (as domain ports).*  

**Finding:** Current ports exist to satisfy **Infrastructure DIP scaffolding ahead of Use Cases**. They are **device secondary ports with SDK vocabulary**, not proven domain ports. That is **intentional interim debt**, not a completed Application boundary.

---

## 3. Application Language Audit

| Symbol | Language | Notes |
|--------|----------|-------|
| `IUhfConnection` | **SDK** | “Uhf” = radio technology |
| `IUhfInventory` | **SDK** | Inventory = Gen2 reader API term |
| `IUhfWriter` / `IUhfReader` | **SDK** | Mirrors Sdk facets 1:1 |
| `IUhfTagControl` | **SDK** | Select/Lock/Kill = vendor ops |
| `TagIdentity` | **SDK** | TagInfo-shaped |
| `TagAccessResponse` | **SDK** | TagResp-shaped |
| `TagReadData` | **SDK** | Read payload mirror |
| `MemBank` | **SDK / Gen2 protocol** | Desk Reader bank ids |
| `DeviceConstants` | Mixed | Timeouts OK; tied to device ops |
| `DeviceResult` | **Business-friendly** | Keep |
| `DeviceErrorCode` | **Business-friendly** | Keep (no STAT_*) |
| `DeviceException` | **Business-friendly** | Keep |
| `Native*` / `STAT_*` / `SdkResult` | N/A in Application | **Not present** — good |

---

## 4. Boundary Decision Matrix

| Current name | SDK or Domain | Use Case proven in code? | Change needed? | Reason |
|--------------|---------------|--------------------------|----------------|--------|
| `IUhfConnection` | SDK | No | **Yes (planned)** | CareHR app must not say “Uhf” in Application |
| `IUhfInventory` | SDK | No | **Yes (planned)** | Prefer scanner/card discovery language |
| `IUhfWriter` | SDK | No | **Yes (planned)** | Prefer `ICardWriter` / write-card port |
| `IUhfReader` | SDK | No | **Yes (planned)** | Prefer `ICardReader` |
| `IUhfTagControl` | SDK | No | **Yes (planned)** | Fold or rename; avoid vendor op dump |
| `TagIdentity` | SDK | No | **Yes (planned)** | → `CardIdentity` (or similar) |
| `TagAccessResponse` | SDK | No | **Yes (planned)** | → write/access result DTO |
| `TagReadData` | SDK | No | **Yes (planned)** | → `CardReadResult` / `CardData` |
| `MemBank` | SDK | No | **Decide with Write Card UC** | Hide behind `WriteEpc(...)` or rename `CardMemoryArea` |
| `DeviceResult` | Domain-ish | Yes (error model) | **No** | Already Application-facing |
| `DeviceErrorCode` | Domain-ish | Yes | **No** | Correct anti-corruption |
| `DeviceException` | Domain-ish | Yes | **No** | Keep |

**Do not rename in Phase 5A** (no Use Case implementation yet — rule 9/10).  
**Do not keep permanently** (scope = B).

---

## 5. DTO Review

| DTO | Mirror SDK? | Allowed to keep as-is? |
|-----|-------------|------------------------|
| `TagIdentity` | Yes | Interim only → migrate |
| `TagAccessResponse` | Yes | Interim only → migrate |
| `TagReadData` | Yes | Interim only → migrate |
| `MemBank` | Yes | Interim; prefer hide in Write Card API |
| `DeviceResult` / `DeviceErrorCode` | No (good) | **Keep** |

No `NativeTag*`, no `STAT_*` on Application surface.

---

## 6. ADR Decision

**Decision: ĐỔI (Domain language required) — not GIỮ.**

- **Not** creating `ADR-006-ApplicationUsesSdkTerminology.md` (that ADR is only for permanent keep).
- Creating **Migration Plan** instead: [ApplicationBoundaryMigrationPlan.md](ApplicationBoundaryMigrationPlan.md).

**Interim acceptance (documented debt):** SDK-shaped ports may remain until Phase 6 Use Case services land, solely to avoid rename-without-consumer. They are **not** the target architecture.

---

## 7. Migration Plan

See [ApplicationBoundaryMigrationPlan.md](ApplicationBoundaryMigrationPlan.md). **No code in Phase 5A.**

---

## 8. Architecture Review Summary

| Topic | Assessment |
|-------|------------|
| Boundary (assemblies) | Correct |
| Domain language | **Not achieved** |
| SDK leakage (types) | Controlled (no Sdk refs, no STAT) |
| SDK leakage (names/concepts) | **Present and intentional interim** |
| Technical debt | Ports ahead of Use Cases; duplicate Sdk names |
| Recommendation | Execute migration **with** WriteCard / Connection / Inventory services — not before |

---

## Blockers for Phase 5 Final Gate

1. Application ports/DTOs still use **SDK language** (`IUhf*`, `Tag*`, `MemBank`).  
2. **No Use Case** implementations exist to prove final domain ports.  
3. Domain-language boundary is **planned** (migration) but **not realized** in code.  
4. Therefore Phase 5A cannot claim “Application is designed in Business Language.”

**Non-blockers (already OK):** no Sdk project reference; no Native/STAT on Application API; Infrastructure DIP intact.
