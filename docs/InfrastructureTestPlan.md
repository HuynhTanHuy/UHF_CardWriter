# Infrastructure Test Plan

**Phase:** 5  
**Scope:** Adapters + mapping (no hardware required for unit tests)  
**Related:** [InfrastructureContract.md](InfrastructureContract.md)

---

## Strategy

| Layer | Technique |
|-------|-----------|
| Unit | Mock `CareHR.UhfCardWriter.Sdk.IUhfSdk` (+ facet mocks) |
| Mapping | Direct tests of `DeviceErrorMapper` / `SdkMapping` (InternalsVisibleTo test project) |
| Smoke | Optional: resolve `AddUhfInfrastructure` + USB count (no Open) |

Do **not** require `UHFPrimeReader` hardware for adapter unit tests.

---

## Mock SDK setup

```text
Mock<IUhfSdk>
  .Connection → Mock<Sdk.IUhfConnection>
  .Inventory  → Mock<Sdk.IUhfInventory>
  .Writer     → Mock<Sdk.IUhfWriter>
  .Reader     → Mock<Sdk.IUhfReader>
  .TagControl → Mock<Sdk.IUhfTagControl>
```

Inject mock into `Uhf*Adapter` constructors.

---

## Cases

### Connection

| Case | Mock SDK returns | Expect |
|------|------------------|--------|
| OpenSerial OK | `SdkResult` success | `DeviceResult.Success`, `ErrorCode.None` |
| OpenSerial fail open | status open-failed | `ReaderOpenFailed` |
| OpenHid handle err | port handle err | `ReaderNotConnected` |
| Close OK | success | Success |
| GetUsbDeviceCount | `SdkResult<int>` value 2 | Value == 2 |
| IsOpen true | true | true |
| SdkException on Open | throws `SdkException` | `DeviceException` |

### Inventory

| Case | Mock | Expect |
|------|------|--------|
| Start OK | success | Success |
| GetCurrentTag OK | TagIdentity payload | App `TagIdentity` EPC copied |
| GetCurrentTag no tag | tag-no-resp / inventory-stop | `TagNotFound` |
| Stop timeout status | comm timeout | `ReaderTimeout` |

### Write

| Case | Mock | Expect |
|------|------|--------|
| Write OK | TagAccessResponse | mapped App response |
| Write pwd err | pwd/auth fail | `InvalidPassword` |
| Write fail | comm write fail | `WriteFailed` |
| SdkException | throw | `DeviceException` |

### Read

| Case | Mock | Expect |
|------|------|--------|
| Read OK | TagReadData | Data/WordCount mapped |
| Read fail | comm read fail | `ReadFailed` |
| Tag no resp | tag-no-resp | `TagNotFound` |

### Select / Lock / Kill

| Case | Mock | Expect |
|------|------|--------|
| Select OK | success | Success |
| Select null mask | ArgumentNullException from SDK/Driver | Pass through Argument* |
| Lock mem locked | ISO mem lock | `TagAccessDenied` |
| Kill OK | success | Success |

### Timeout / disconnect

| Case | Vendor status | Expect ErrorCode |
|------|---------------|------------------|
| Comm timeout | `0xFFFFFF12` | `ReaderTimeout` |
| Disconnect | `0xFFFFFF17` | `ReaderDisconnected` |
| Unconnect | `0xFFFFFF16` | `ReaderNotConnected` |

### Dispose

| Case | Expect |
|------|--------|
| SDK disposed → facet call | `ObjectDisposedException` or `DeviceException` (as thrown) |
| Provider dispose | `IUhfSdk.Dispose` invoked (integration) |

### Unknown status

| Case | Expect |
|------|--------|
| Unmapped status code | `DeviceErrorCode.Unknown`, Message preserved |

---

## Explicit non-tests (out of Infrastructure)

| Concern | Owner |
|---------|-------|
| Inventory poll until one tag | Application service |
| Verify EPC match | Verify service |
| Retry / reconnect | Later policy phase |
| UI state machine | App / UI phase |

---

## Suggested project layout (later)

```
tests/CareHR.UhfCardWriter.Infrastructure.Tests/
  Devices/DeviceErrorMapperTests.cs
  Devices/UhfConnectionAdapterTests.cs
  ...
```

`InternalsVisibleTo` for mapper tests if kept `internal`.
