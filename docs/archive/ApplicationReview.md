# Application Review — Phase 6C

**Date:** 2026-08-06  
**Layer:** `CareHR.UhfCardWriter.Application`

---

## Service Boundary

| Service | SRP | UC evidence |
|---------|-----|-------------|
| `CardConnectionService` | Connection + USB list | UC-001–003 |
| `CardScanningService` | Single-card scan + select + stop | UC-004, 005, 010 |
| `CardReadingService` | Read identity | UC-006 |
| `CardWritingService` | Write identity | UC-007 |
| `CardVerificationService` | Compare intended vs read | UC-008 |
| `CardRegistrationService` | Registry after verify | UC-009 |
| `CardWriteOrchestrator` | Workflow only | UC-004→009, 010 |

`CardSecurityService` **not** implemented (UC-011/012 deferred per ApplicationServices.md).

---

## Workflow

Matches [ApplicationWorkflow.md](ApplicationWorkflow.md): Scan → Select → Write → Verify → Register.  
Connect is precondition of the job (Operator connects via `CardConnectionService` first).

---

## Validation & Business Rules

Documented in [BusinessRules.md](BusinessRules.md). Enforced in services/orchestrator via exceptions + result types.

---

## Coupling

```text
Services → ICard* ports + Models
Orchestrator → Services only (no ports, no SDK)
Application → no Infrastructure / Sdk / Driver / Native project refs
```

---

## Technical Debt / Known Limitations

| ID | Item | Notes |
|----|------|-------|
| A-TD-01 | `ICardRegistrar` has no Infrastructure adapter yet | Phase 6C forbids Infra edits; wire HTTP adapter next |
| A-TD-02 | Application Services not registered in DI | App/Infra DI extension later |
| A-TD-03 | Lock/Kill services deferred | UC-011/012 |
| A-TD-04 | Hospital/group EPC composition rules | Not specified in UC; payload accepted as bytes |
| A-TD-05 | `DeviceException` still exists for port boundary | Services map to results / `OperationException`; UI must not catch `DeviceException` as primary API |

---

## Risks

| Risk | Mitigation |
|------|------------|
| Scan poll window may miss intermittent tags | Configurable `ScanTimeoutMs` on job request |
| Register without Infra adapter | Blocker for **runtime** register only — Application contract complete |
| Thread.Sleep in scan loop | Acceptable for WinForms desk app; no async required by UC docs |

---

## Recommendation

1. Next: Infrastructure `ICardRegistrar` adapter + DI for Application Services.  
2. Then: UI calls `CardConnectionService` + `CardWriteOrchestrator`.  
3. Add unit tests per [ApplicationTestPlan.md](ApplicationTestPlan.md).

---

## Gate assessment

Application Services + workflow + rules docs are complete for Phase 6C.  
Runtime register requires Infra adapter (out of 6C scope) — not an Application-layer blocker.
