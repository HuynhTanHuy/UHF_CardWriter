# Resource Lifetime — Driver

**Phase:** 3  
**Types:** `UhfPrimeDriver`, `NativeBuffer`  
**Related:** [ADR-003](adr/ADR-003-Handle-Management.md), [NativeBufferPolicy.md](NativeBufferPolicy.md)

---

## Handle lifetime

| State | `_handle` | `IsOpen` |
|-------|-----------|----------|
| New / after failed Open / after Close / after Dispose | `IntPtr.Zero` | `false` |
| After successful Open* | non-zero | `true` |

**Ownership:** Only `UhfPrimeDriver` owns the handle. Never returned publicly.

**Acquisition:** `OpenDevice` / `OpenHid` / `OpenNet` store handle **only if** status == OK.

**Release:**

1. Preferred: `Close()` → `CloseDevice` → clear handle → return status  
2. `Dispose()` → best-effort `CloseDevice` → clear handle → mark disposed (no throw)

**Rule:** Do not leak: every successful Open must eventually Close or Dispose.

---

## Buffer lifetime

| Buffer | Lifetime |
|--------|----------|
| IN `byte[]` from caller | Owned by caller; pinned only for P/Invoke duration |
| `NativeBuffer` | Created in `GetReadTagResp`, disposed in `using` before return |
| OUT payload in `TagReadNative.Data` | Independent copy; caller owns after return |
| `StringBuilder` in `GetHidUsbInfo` | Local; string copied out |

No `AllocHGlobal` today → no unmanaged free paths in Driver.

---

## Dispose / using

```csharp
using var driver = new UhfPrimeDriver();
var open = driver.OpenHid(0);
// ... single-call APIs ...
driver.Close(); // optional if using Dispose
```

| API | Notes |
|-----|-------|
| `IDisposable` | Supported |
| Idempotent Dispose | Yes |
| Finalizer | None — callers must Dispose/using |

---

## Sequence — open / operate / close

```mermaid
sequenceDiagram
    participant C as Caller
    participant D as UhfPrimeDriver
    participant N as UhfPrimeNative
    participant DLL as UHFPrimeReader.dll

    C->>D: OpenHid(index)
    D->>N: OpenHidConnection
    N->>DLL: OpenHidConnection
    DLL-->>N: status + handle
    N-->>D: status + handle
    alt status OK
        D->>D: _handle = handle
    else status fail
        D->>D: _handle remains 0
    end
    D-->>C: NativeResult

    C->>D: InventoryContinue / WriteTag / ...
    D->>D: RequireHandle
    D->>N: P/Invoke
    N->>DLL: API
    DLL-->>N: status (+ structs)
    N-->>D: status
    D->>D: marshal if needed
    D-->>C: NativeResult / NativeResult of T

    C->>D: Close()
    D->>N: CloseDevice(_handle)
    N->>DLL: CloseDevice
    D->>D: _handle = 0
    D-->>C: NativeResult

    C->>D: Dispose()
    Note over D: no-op if already closed
```

---

## Sequence — GetReadTagResp buffer

```mermaid
sequenceDiagram
    participant D as UhfPrimeDriver
    participant B as NativeBuffer
    participant N as UhfPrimeNative

    D->>B: new NativeBuffer(maxDataBytes)
    D->>N: GetReadTagResp(..., buffer.Buffer, ...)
    N-->>D: status, wordCount, filled buffer
    alt success
        D->>B: ToArray(byteCount)
        B-->>D: copy byte[]
        D->>D: MapTagResp → TagReadNative
    end
    D->>B: Dispose (using)
```

---

## Lifetime hazards (documented, not changed)

| Hazard | Severity | Notes |
|--------|----------|-------|
| `Close()` clears handle even if `CloseDevice` returns error | Medium | May orphan native resources if SDK reports fail but still needs retry — report only |
| Forget Dispose after successful Open | High for process | Use `using` |
| Use after Dispose | Guarded | `ObjectDisposedException` |
