# Infrastructure — Adapters & DI

Maps Application `ICard*` ports to SDK facets and CareHR HTTP.
Living document — phase reports archived under docs/archive/.

**Assembly:** `CareHR.UhfCardWriter.Infrastructure`  
**Registration:** `AddUhfInfrastructure` / `AddCareHrCardWriter`

---

## Common rules

| Rule | Behavior |
|------|----------|
| SDK | Device adapters call **only** `IUhfSdk` facets |
| Application | Implement `ICard*` ports; return domain/device results (`DeviceResult`, `Card*Result`, …) |
| Forbidden | Business orchestration, retry policy, logging, verify, inventory poll loops, `DllImport` |
| Thread safety | **Not** thread-safe (serialize above Infrastructure) |
| Lifetime | Shared **singleton** `IUhfSdk` + singleton adapters |

---

## Port → Adapter contracts

Names below match current DI (`Card*Adapter` / `ICard*`). Older phase docs that said `Uhf*Adapter` / `IUhf*` are obsolete for Application ports.

### `CardConnectionAdapter` → `ICardConnection`

| Field | Contract |
|-------|----------|
| **Responsibilities** | OpenSerial / OpenHid / OpenNet / Close; IsOpen; USB count/info; map to connection/`DeviceResult` |
| **Non-responsibilities** | Inventory, write/read, RF power, reconnect policy |
| **Dependencies** | `IUhfSdk.Connection` |

### `CardScannerAdapter` → `ICardScanner`

| Field | Contract |
|-------|----------|
| **Responsibilities** | Start / Stop / TryGetCard (single calls); SelectByIdentity; map to `CardInformation` / scan results |
| **Non-responsibilities** | Poll-until-found policy owned by Application services; multi-tag business rules |
| **Dependencies** | `IUhfSdk.Inventory` + `TagControl.Select` |

### `CardWriterAdapter` → `ICardWriter`

| Field | Contract |
|-------|----------|
| **Responsibilities** | Write EPC identity (Gen2 EPC bank, `wordPtr=2`); map write outcome / errors |
| **Non-responsibilities** | Select, verify, InventoryStop, retry |
| **Dependencies** | `IUhfSdk.Writer` |

### `CardReaderAdapter` → `ICardReader`

| Field | Contract |
|-------|----------|
| **Responsibilities** | Read EPC identity; map to `CardReadResult` |
| **Non-responsibilities** | Compare expected EPC / verify |
| **Dependencies** | `IUhfSdk.Reader` |

### `CardSecurityAdapter` → `ICardSecurity`

| Field | Contract |
|-------|----------|
| **Responsibilities** | Lock / Kill |
| **Non-responsibilities** | GetTagResp composition for lock/kill; kill confirmation UX |
| **Dependencies** | `IUhfSdk.TagControl` |

### `HttpCardRegistrarAdapter` → `ICardRegistrar`

| Field | Contract |
|-------|----------|
| **Responsibilities** | `POST {BaseUrl}/api/rfid/cards` with CareHR create-card body; map to `RegistrationResult` |
| **Non-responsibilities** | Verify-before-register (Application); SDK types |
| **Dependencies** | `CareHrCardApiOptions` + `HttpClient` |

See [API.md](API.md) for the production wire contract.

---

## Exception / result mapping

| Source | Adapter behavior |
|--------|------------------|
| Vendor status on `SdkResult` | `DeviceResult` + `DeviceErrorCode` (no throw) |
| `SdkException` | `DeviceException` (via `DeviceExceptionTranslator`) |
| `Argument*` / `ObjectDisposedException` | Pass through |
| HTTP non-success / network | `RegistrationResult.Fail` |

| Downstream | Application |
|------------|-------------|
| `SdkResult` + STAT_* | `DeviceResult` + `DeviceErrorCode` |
| SDK tag identity / access / read | `CardInformation` / `CardIdentity` / `CardWriteResult` / `CardReadResult` |

Internal helpers: `SdkMapping`, `DeviceErrorMapper`, `DeviceExceptionTranslator`.

---

## Dependency map

```text
Application (Services → ICard* ports)
        ↓
Infrastructure (Card*Adapter / HttpCardRegistrarAdapter)
        ↓
SDK Wrapper (IUhfSdk)  |  CareHR HTTP API
        ↓
Driver → Native → UHFPrimeReader.dll
```

| Application port | Adapter | Downstream |
|------------------|---------|------------|
| `ICardConnection` | `CardConnectionAdapter` | `IUhfSdk.Connection` |
| `ICardScanner` | `CardScannerAdapter` | Inventory + `TagControl.Select` |
| `ICardWriter` | `CardWriterAdapter` | `Writer.Write` (EPC bank, wordPtr=2) |
| `ICardReader` | `CardReaderAdapter` | `Reader.Read` (EPC bank, wordPtr=2) |
| `ICardSecurity` | `CardSecurityAdapter` | `TagControl.Lock` / `Kill` |
| `ICardRegistrar` | `HttpCardRegistrarAdapter` | `POST {BaseUrl}/api/rfid/cards` |

---

## DI lifetime

| Service | Implementation | Lifetime |
|---------|----------------|----------|
| `IUhfSdk` | `UhfPrimeSdk` | **Singleton** |
| `ICardConnection` | `CardConnectionAdapter` | **Singleton** |
| `ICardScanner` | `CardScannerAdapter` | **Singleton** |
| `ICardWriter` | `CardWriterAdapter` | **Singleton** |
| `ICardReader` | `CardReaderAdapter` | **Singleton** |
| `ICardSecurity` | `CardSecurityAdapter` | **Singleton** |
| `ICardRegistrar` | `HttpCardRegistrarAdapter` | **Singleton** |
| `CareHrCardApiOptions` | options instance | **Singleton** |

**Why Singleton:** one native handle per process; WinForms host is process-wide; adapters are stateless holders of `IUhfSdk`; matches one SDK ↔ one Driver ↔ one reader session.

**Not Scoped/Transient:** classic WinForms has no request scope; Transient would create multiple Drivers/handles and unclear Dispose ownership.

**Dispose:** `UhfPrimeSdk` is `IDisposable`; MS.DI disposes singleton when root `ServiceProvider` is disposed. Prefer explicit `Connection.Close()` when status matters.

**Multi-reader (future):** keyed DI, SDK factory, or separate hosts — do **not** switch to Transient SDK without a factory ADR. Singleton ≠ thread-safe; callers still serialize.

---

## Related

[Application.md](Application.md) · [API.md](API.md) · [SDK.md](SDK.md) · [Architecture.md](Architecture.md) · [Configuration.md](Configuration.md)
