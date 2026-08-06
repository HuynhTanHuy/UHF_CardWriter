# Business Rules — CareHR Card Writer

**Phase:** 6C  
**Source:** [ApplicationUseCases.md](ApplicationUseCases.md), [ApplicationWorkflow.md](ApplicationWorkflow.md), [DomainModel.md](DomainModel.md)

---

| ID | Rule | Source Use Case | Affected Service |
|----|------|-----------------|------------------|
| **BR-001** | Reader must be Connected before Scan / Select / Read / Write / Verify. | UC-001 precondition for UC-004–008 | `CardConnectionService`, `CardScanningService`, `CardReadingService`, `CardWritingService`, `CardWriteOrchestrator` |
| **BR-002** | Write job requires exactly one distinct card in the RF field. | UC-004 | `CardScanningService`, `CardWriteOrchestrator` |
| **BR-003** | Verify is mandatory after successful Write before Register. | UC-008, Workflow | `CardVerificationService`, `CardWriteOrchestrator` |
| **BR-004** | Register is allowed only after successful Verify (`IsVerified=true`). | UC-009 | `CardRegistrationService`, `CardWriteOrchestrator` |
| **BR-005** | Lock requires Administrator / trained Operator (deferred UI). | UC-011 | *Deferred — no `CardSecurityService` in 6C MVP* |
| **BR-006** | Kill requires explicit confirmation (deferred UI). | UC-012 | *Deferred — no `CardSecurityService` in 6C MVP* |
| **BR-007** | Access password must be exactly 4 bytes. | UC-006, UC-007, UC-008 | `CardReadingService`, `CardWritingService`, `CardVerificationService` |
| **BR-008** | Intended EPC must be non-empty and even-length (word-aligned). | UC-007 | `CardWritingService`, `CardWriteOrchestrator` |
| **BR-009** | On Register API failure after Verify: state **WrittenButUnregistered** — do not auto-rewrite. | UC-009, Workflow | `CardWriteOrchestrator` |
| **BR-010** | Cancel (UC-010) stops inventory best-effort and must not register. | UC-010 | `CardScanningService`, `CardWriteOrchestrator` |
| **BR-011** | Card type id and batch code required for Register. | UC-009 | `CardRegistrationService`, `CardWriteOrchestrator` |

---

## Enforcement style

| Kind | Mechanism |
|------|-----------|
| Preconditions | `BusinessException` / `ValidationException` or `DeviceResult` / job result fail |
| Device outcomes | `DeviceResult` / `ScanResult` / `CardVerifyResult` / `RegistrationResult` |
| Orchestration policy | `CardWriteJobResult.Stage` (`WrittenButUnregistered`, `Cancelled`, …) |
