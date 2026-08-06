# Application DTOs — Discovery

**Phase:** 6A  
**Rule:** DTOs come from Ports/Services; **forbidden tokens:** Uhf, Tag (as SDK noun), Native, STAT, MemBank, Sdk  
**Related:** [ApplicationPorts.md](ApplicationPorts.md)

---

## Connection / reader

| DTO | Used by |
|-----|---------|
| **ReaderEndpoint** | Connect inputs (COM+baud / USB index / IP+port+timeout) |
| **ReaderInformation** | USB list item (index, display name) |
| **ReaderStatus** | Connected flag / human status |
| **ConnectionResult** | Outcome of connect/disconnect (`OperationResult`) |

---

## Card identity / IO

| DTO | Used by |
|-----|---------|
| **CardIdentity** | EPC identity (hex string + raw bytes as needed) |
| **CardInformation** | Identity + RSSI/antenna metadata from scan (business-safe fields only) |
| **CardReadResult** | Outcome of read + identity/content |
| **CardWriteRequest** | Intended identity + password + job metadata refs |
| **CardWriteResult** | Device write outcome |
| **CardVerifyRequest** | Intended vs actual identity |
| **CardVerifyResult** | Match boolean + details |
| **ScanResult** | Zero/one/many + `CardInformation` when one |

---

## Registration / job

| DTO | Used by |
|-----|---------|
| **RegistrationRequest** | Identity + card type id + batch code |
| **RegistrationResult** | API success/failure message |
| **CardWriteJobRequest** | Operator snapshot: endpoint already connected; patient/business fields; type; batch; password |
| **CardWriteJobResult** | End-to-end success + stage that failed + messages |

---

## Shared result

| DTO | Used by |
|-----|---------|
| **OperationResult** | Step success + `DeviceErrorCode` + message (reuse existing Application error enum) |

---

## Forbidden on Application DTOs

| Forbidden | Replacement |
|-----------|-------------|
| `TagIdentity` | `CardIdentity` / `CardInformation` |
| `TagAccessResponse` | `CardWriteResult` |
| `TagReadData` | `CardReadResult` |
| `MemBank` | Hidden in Infrastructure |
| `SdkResult` / `STAT_*` | `OperationResult` / `DeviceErrorCode` |
| `Uhf*` type names | `Card*` / `Reader*` |

---

## Note on existing interim types

Current Application code may still contain `IUhf*` / `Tag*` from Phase 5 scaffolding. Phase 6A **does not rename**. Migration executes in Phase 6B+ per [ApplicationMigrationPlan.md](ApplicationMigrationPlan.md).
