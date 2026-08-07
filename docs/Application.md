# Application — CareHR Card Writer

Domain model, use cases, services, ports, workflow, and runtime wiring for the card-write job.
Living document — phase reports archived under docs/archive/.

---

## Domain model

| Object | Meaning |
|--------|---------|
| **Card** | Physical CareHR RFID card being issued/updated |
| **CardIdentity** | Business identity of the card as EPC string/bytes |
| **CardContent** | Readable payload (primarily EPC) |
| **IntendedCardIdentity** | EPC the Operator/job wants written |
| **CurrentCardIdentity** | EPC currently on the physical card |
| **CardWriteJob** | One Operator attempt: scan → write → verify → register |
| **CardRegistration** | Backend persistence of a verified card identity |
| **CardBatch** | Issuance batch metadata (`RfidCardBatchCode`) |
| **CardType** | CareHR RFID card type (`RfidCardTypeId`) |
| **Reader** / **ReaderConnection** / **ReaderEndpoint** | Desk reader device, session, and how to reach it |
| **ReaderStatus** | Connected / Disconnected / Busy / Error |
| **OperationOutcome** | Success/failure of a business step |
| **OperatorSession** | UI session context |

```text
OperatorSession
  └─ ReaderConnection ──► Reader (via ReaderEndpoint)
  └─ CardWriteJob
        ├─ CurrentCardIdentity (from scan)
        ├─ IntendedCardIdentity (from CareHR input)
        ├─ CardBatch / CardType
        ├─ verify result
        └─ CardRegistration (after API)
```

**Non-domain** (keep out of Application language): MemBank, wordPtr, InventoryContinue, NativeTagInfo, STAT_*, TagInfo/TagResp, DEA_*/MIFARE.

**Invariants:** no register without verify; prefer single card in field; physical write before CareHR register; API failure after verify ≠ automatic rewrite.

---

## Business rules

| ID | Rule | Enforcement |
|----|------|-------------|
| **BR-001** | Reader Connected before Scan / Select / Read / Write / Verify | Connection + device services |
| **BR-002** | Write job requires exactly one distinct card in RF field | `CardScanningService`, orchestrator |
| **BR-003** | Verify mandatory after successful Write before Register | `CardVerificationService`, orchestrator |
| **BR-004** | Register only when `IsVerified=true` | `CardRegistrationService` |
| **BR-005** | Lock requires Administrator / trained Operator | Deferred (no UI MVP) |
| **BR-006** | Kill requires explicit confirmation | Deferred |
| **BR-007** | Access password exactly 4 bytes | Read / Write / Verify |
| **BR-008** | Intended EPC non-empty and even-length (word-aligned) | Write / orchestrator |
| **BR-009** | Register API fail after Verify → **WrittenButUnregistered**; no auto-rewrite | Orchestrator |
| **BR-010** | Cancel stops inventory best-effort; must not register | Scan / orchestrator |
| **BR-011** | Card type id and batch code required for Register | Registration / orchestrator |

---

## Use cases

| UC | Goal | Notes |
|----|------|-------|
| **UC-001** Connect Reader | Open COM / USB HID / Network session | Precondition for device ops |
| **UC-002** Disconnect Reader | Close session (idempotent) | |
| **UC-003** Discover Readers (USB) | List HID readers | Session need not be open |
| **UC-004** Scan Card | Exactly one EPC in field | Fail on none / many |
| **UC-005** Select Card | Restrict access by EPC mask | System step before write |
| **UC-006** Read Card | Read EPC / content | Also used by Verify |
| **UC-007** Write Card | Write intended CareHR EPC | Password + validated EPC |
| **UC-008** Verify Card | Read-back == intended | **Must not** register on fail |
| **UC-009** Register Card | Persist to CareHR API | After verify only |
| **UC-010** Cancel Operation | Stop long op; no partial registry | |
| **UC-011** Lock Card | Gen2 lock | Optional / deferred UI |
| **UC-012** Kill Card | Permanent disable | High risk / deferred |

**Out of scope:** MIFARE, warehouse multi-tag portal, multi-reader farm.

---

## Services

| Service | Use cases | Responsibility |
|---------|-----------|----------------|
| `CardConnectionService` | UC-001–003 | Connect / disconnect / USB list / status |
| `CardScanningService` | UC-004, 005, 010 | Single-card scan, select, stop |
| `CardWritingService` | UC-007 | Write intended identity |
| `CardReadingService` | UC-006 | Read identity/content |
| `CardVerificationService` | UC-008 | Compare intended vs read |
| `CardRegistrationService` | UC-009 | CareHR registry after verify |
| `CardWriteOrchestrator` | UC-004→009, 010 | End-to-end write job (not a device port) |

Deferred: `CardSecurityService` (UC-011/012). Reject SDK-named services (`UhfInventoryService`, etc.).

MVP UI typically calls `RunWriteCardJob` on the orchestrator only.

---

## Ports (`ICard*`)

| Port | Consumer | Capability |
|------|----------|------------|
| `ICardConnection` | Connection | OpenSerial / OpenHid / OpenNet / Close / IsOpen / ListUsbReaders |
| `ICardScanner` | Scanning | StartScan / TryGetCard / StopScan / SelectByIdentity |
| `ICardWriter` | Writing | WriteIdentity (EPC + password) — **no MemBank in signature** |
| `ICardReader` | Reading / Verify | ReadIdentity |
| `ICardRegistrar` | Registration | Register(identity, type, batch, …) |
| `ICardSecurity` | Security (optional) | Lock / Kill |

Infrastructure adapts these → `IUhfSdk` (or HTTP for registrar). Prefer `ICard*` over `IUhf*` / `IUhfInventory`.

---

## DTOs (Application language)

| Area | Types |
|------|-------|
| Connection | `ReaderEndpoint`, `ReaderInformation`, `ReaderStatus`, `ConnectionResult` / `OperationResult` |
| Card IO | `CardIdentity`, `CardInformation`, `CardReadResult`, `CardWriteRequest`/`Result`, `CardVerifyRequest`/`Result`, `ScanResult` |
| Job / API | `RegistrationRequest`/`Result`, `CardWriteJobRequest`/`Result` |

**Forbidden on Application DTOs:** `Uhf*`, SDK `Tag*` nouns, Native, STAT, MemBank, Sdk result types.

---

## Workflow (happy path)

```text
Connect → [Discover USB] → Scan (one EPC) → Select
  → Write intended EPC → Verify (read-back) → Register → Complete
```

Error policy highlights: write/verify fail → **do not** register; register fail after verify → **WrittenButUnregistered** (retry register only); cancel → stop inventory, no register.

### Write job stages (`CardWriteJobStage`)

```text
Idle → Scanning → Selecting → Writing → Verifying → Registering → Completed
Failures → Failed | WrittenButUnregistered | Cancelled → Idle
```

Connect/Disconnect sit outside the job stage enum when the reader is already Connected. Lock/Kill not in MVP state machine.

### Sequence (orchestrator)

`RunWriteCardJob` → `IsConnected` → `ScanForSingleCard` → `SelectCard` → `WriteIdentity` → `Verify` → `Register(IsVerified=true)` → `CardWriteJobResult`.

---

## Layer boundary

| Layer | Language |
|-------|----------|
| Application | Card / CareHR domain (`ICard*`, Verify, Register) |
| Infrastructure | Adapters + mapping |
| SDK | `IUhfSdk`, `MemBank`, `SdkResult` |
| Driver / Native | `UhfPrimeDriver`, `NativeResult`, `STAT_*` |

```text
Operator → Application Service → ICard* port → Adapter → IUhfSdk → Driver → Native → Reader → Card
Registration: Application → ICardRegistrar → HttpCardRegistrarAdapter → CareHR API
```

---

## Runtime / DI graph

```text
UI / CompositionRoot
  → Application Services (Singleton): Connection, Scanning, Reading, Writing,
    Verification, Registration, CardWriteOrchestrator
  → ICard* ports
  → Card*Adapter / HttpCardRegistrarAdapter
  → IUhfSdk (UhfPrimeSdk) | CareHR HTTP
  → UhfPrimeDriver → UHFPrimeReader.dll
```

| Port | Adapter | Downstream |
|------|---------|------------|
| `ICardConnection` | `CardConnectionAdapter` | `IUhfSdk.Connection` |
| `ICardScanner` | `CardScannerAdapter` | Inventory + `TagControl.Select` |
| `ICardWriter` | `CardWriterAdapter` | `Writer.Write` (EPC) |
| `ICardReader` | `CardReaderAdapter` | `Reader.Read` (EPC) |
| `ICardSecurity` | `CardSecurityAdapter` | `TagControl.Lock/Kill` |
| `ICardRegistrar` | `HttpCardRegistrarAdapter` | `POST /api/rfid/cards` |

| Method | Project | Registers |
|--------|---------|-----------|
| `AddApplicationServices` | Application | Services + Orchestrator |
| `AddUhfInfrastructure` | Infrastructure | `IUhfSdk`, `ICard*` adapters, API options |
| `AddCareHrCardWriter` | Infrastructure | Both |
| `CompositionRoot.CreateServiceProvider` | App | Host composition |

Exception path: Native/Driver → `SdkException` → `DeviceException` → `DeviceResult` / Application outcomes. HTTP registry failures → `RegistrationResult.Fail` (no SDK types).

---

## Related living docs

[Architecture.md](Architecture.md) · [Infrastructure.md](Infrastructure.md) · [API.md](API.md) · [Driver.md](Driver.md) · [SDK.md](SDK.md)
