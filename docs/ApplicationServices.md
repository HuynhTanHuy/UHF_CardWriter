# Application Services — Discovery

**Phase:** 6A  
**Rule:** A service exists only if justified by one or more Use Cases  
**Related:** [ApplicationUseCases.md](ApplicationUseCases.md)

---

## Proposed services

| Service | Justified by Use Cases | Responsibility |
|---------|------------------------|----------------|
| **CardConnectionService** | UC-001, UC-002, UC-003 | Connect/disconnect; list USB readers; expose connection status |
| **CardScanningService** | UC-004, UC-005, UC-010 (stop) | Scan for one card; apply select; cancel/stop scan |
| **CardWritingService** | UC-007 (+ orchestrates select if needed) | Write intended identity to card |
| **CardReadingService** | UC-006 | Read card identity/content |
| **CardVerificationService** | UC-008 | Compare intended vs read identity |
| **CardRegistrationService** | UC-009 | Call CareHR registry after verify |
| **CardWriteOrchestrator** *(Application workflow)* | UC-004→009, UC-010 | Single “Write Card job” coordinating the above — **not** a device port |

---

## Optional / deferred services

| Service | Use Cases | When |
|---------|-----------|------|
| **CardSecurityService** | UC-011, UC-012 | Only if product enables Lock/Kill in UI |

---

## Explicit non-services

| Name | Why rejected |
|------|--------------|
| `UhfInventoryService` | SDK language |
| `MemBankService` | Protocol detail |
| Service with no UC | Forbidden |

---

## Orchestration note

MVP UI typically calls **one** orchestrator use-case method (`RunWriteCardJob`) which internally uses connection/scan/write/verify/register services.  
Device-facing ports remain under the specialized services — orchestrator does not call SDK.
