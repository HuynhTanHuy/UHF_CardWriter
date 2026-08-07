# Application Unit Test Plan — Phase 6C

**Scope:** Application Services + Orchestrator (ports mocked). No SDK/Driver/Native.

---

## Fixture

| Double | Role |
|--------|------|
| `FakeCardConnection` | `ICardConnection` |
| `FakeCardScanner` | `ICardScanner` |
| `FakeCardWriter` | `ICardWriter` |
| `FakeCardReader` | `ICardReader` |
| `FakeCardRegistrar` | `ICardRegistrar` |

---

## Cases

### Connection (UC-001–003)

| ID | Case | Expect |
|----|------|--------|
| T-CONN-01 | Connect Serial valid | Success; `IsConnected` |
| T-CONN-02 | Connect with empty COM | `ValidationException` |
| T-CONN-03 | Disconnect when open | Success |
| T-CONN-04 | Disconnect when already closed | Success (idempotent) |
| T-CONN-05 | List USB when count=2 | Two `ReaderInformation` |
| T-CONN-06 | Open fails from port | `DeviceResult` fail / `ReaderOpenFailed` |

### Scan (UC-004–005, UC-010)

| ID | Case | Expect |
|----|------|--------|
| T-SCAN-01 | One unique EPC in window | `ScanOutcome.Single` |
| T-SCAN-02 | No card | `TagNotFound` |
| T-SCAN-03 | Two distinct EPCs | `MultipleCardsDetected` (BR-002) |
| T-SCAN-04 | Scan while disconnected | `BusinessException` (BR-001) |
| T-SCAN-05 | Select after scan | Select port called with identity |
| T-SCAN-06 | Cancel mid-scan | `ScanOutcome.Cancelled`; StopScan called |

### Read (UC-006)

| ID | Case | Expect |
|----|------|--------|
| T-READ-01 | Valid read | `CardIdentity` from data |
| T-READ-02 | Bad password length | `ValidationException` (BR-007) |
| T-READ-03 | Read fail from port | `ReadFailed` |

### Write (UC-007)

| ID | Case | Expect |
|----|------|--------|
| T-WRITE-01 | Valid write | Success payload |
| T-WRITE-02 | Odd-length EPC | `ValidationException` (BR-008) |
| T-WRITE-03 | Write fail from port | `WriteFailed`; no register later |
| T-WRITE-04 | Disconnected | `BusinessException` (BR-001) |

### Verify (UC-008)

| ID | Case | Expect |
|----|------|--------|
| T-VER-01 | Read matches intended | `IsMatch=true` |
| T-VER-02 | Read mismatches | `VerificationFailed` |
| T-VER-03 | Read fails | Fail; not Success |

### Register (UC-009)

| ID | Case | Expect |
|----|------|--------|
| T-REG-01 | Verified + API OK | Success |
| T-REG-02 | `IsVerified=false` | `BusinessException` (BR-004) |
| T-REG-03 | Missing type/batch | `ValidationException` (BR-011) |
| T-REG-04 | API fail | `RegistrationResult` fail |

### Orchestrator

| ID | Case | Expect |
|----|------|--------|
| T-ORCH-01 | Happy path | `Completed` |
| T-ORCH-02 | Multiple cards | Failed at Scanning |
| T-ORCH-03 | Reader disconnect / not connected | Failed `ReaderNotConnected` |
| T-ORCH-04 | Write fail | Failed at Writing; registrar never called |
| T-ORCH-05 | Verify fail | Failed at Verifying; registrar never called |
| T-ORCH-06 | Register fail after verify | `WrittenButUnregistered` (BR-009) |
| T-ORCH-07 | Cancel | `Cancelled`; StopScan |

---

## Non-goals for this plan

- Real USB/COM hardware  
- HTTP integration  
- Infra adapter unit tests (separate)
