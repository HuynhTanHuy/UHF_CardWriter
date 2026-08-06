# Application Runtime Review — Phase 6D

**Date:** 2026-08-06  
**Scope:** Runtime integration only — no new business rules / workflow / UI.

---

## Runtime Readiness

| Item | Status |
|------|--------|
| All `ICard*` ports have adapters | Yes (incl. `ICardRegistrar` → `HttpCardRegistrarAdapter`) |
| Application Services registered in DI | Yes (`AddApplicationServices`) |
| Full composition API | Yes (`AddCareHrCardWriter`, `CompositionRoot`) |
| SDK / Driver / Native unchanged | Yes |
| Application Service logic unchanged | Yes |
| Smoke: DI resolve | `tools/Phase6DSmoke` |
| Smoke: mocked Write→Verify→Register | `tools/Phase6DSmoke` |

---

## Dependency Readiness

| Gap from 6C | Resolution in 6D |
|-------------|------------------|
| A-TD-01 `ICardRegistrar` missing | `HttpCardRegistrarAdapter` |
| A-TD-02 Services not in DI | `AddApplicationServices` + `AddCareHrCardWriter` |

---

## DI Review

| Registration | Lifetime | Notes |
|--------------|----------|-------|
| `IUhfSdk` + device adapters | Singleton | Single desk-reader session; not thread-safe |
| `ICardRegistrar` / `HttpClient` | Singleton | Desk-app pattern; serialize access |
| Application Services | Singleton | Align with session; Orchestrator is orchestration only |
| Circular dependencies | None | Services → Ports → Adapters |

---

## Known Limitations / Technical Debt

| ID | Item | Notes |
|----|------|-------|
| R-TD-01 | App `Program.cs` still runs placeholder `Form1` without resolving DI | Phase 7 UI wiring |
| R-TD-02 | API BaseUrl/Token not loaded from config/env yet | Pass via `AddCareHrCardWriter(configure)` |
| R-TD-03 | Live hardware + live API not part of automated smoke | Mocked ports for CI; hardware smoke is manual |
| R-TD-04 | `ICardSecurity` registered but no Application Security service | Deferred UC-011/012 |
| R-TD-05 | `HttpClient.Send` sync | Matches CardWritter desktop pattern |

---

## Risks

| Risk | Mitigation |
|------|------------|
| Unconfigured API URL | Adapter returns `RegistrationResult.Fail` (WrittenButUnregistered path) |
| Singleton SDK shared incorrectly across threads | Document serialize access; UI single-threaded STA |
| Fake smoke ≠ hardware truth | Phase 7+ manual device validation |

---

## Recommendation

1. Phase 7: WinForms resolves `CardConnectionService` + `CardWriteOrchestrator` from `CompositionRoot`.  
2. Load `CareHrCardApiOptions` from appsettings/user secrets.  
3. Keep unit tests from ApplicationTestPlan with mocked ports.

---

## Gate

Runtime graph is complete; composition resolves end-to-end; business/workflow unchanged.
