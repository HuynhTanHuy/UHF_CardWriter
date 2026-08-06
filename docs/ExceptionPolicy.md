# Exception Policy — Driver Layer

**Phase:** 3  
**Scope:** `CareHR.UhfCardWriter.Sdk.Driver`  
**Related:** [DriverContract.md](DriverContract.md), [NativeResultReview.md](NativeResultReview.md)

---

## Principle

| Outcome | Mechanism |
|---------|-----------|
| Expected SDK / device / tag outcomes | **Return** `NativeResult` / `NativeResult<T>` |
| Programmer misuse / resource / interop hard failure | **Throw** |

SDK `STAT_*` codes are **not** exceptions.

---

## Return `NativeResult` when

- DLL returns a status code after a completed P/Invoke
- Open fails (`Port handle error`, open failed, etc.)
- Inventory / tag timeouts, no tag, password wrong, memory locked, etc.
- Close returns a non-OK status
- HID count/info returns a negative/error status

Caller (Phase 4+) decides retry/UI messaging.

---

## Throw when

| Condition | Exception type |
|-----------|----------------|
| Driver instance disposed | `ObjectDisposedException` |
| Operation requires open handle but none | `NativeException` |
| Open while already open | `NativeException` |
| Null / invalid managed arguments | `ArgumentNullException` / `ArgumentException` / `ArgumentOutOfRangeException` |
| Marshal mapping failure after successful SDK status | `NativeException` (with inner) |
| DLL missing / BadImage / entry point (CLR) | `DllNotFoundException` / `EntryPointNotFoundException` / `BadImageFormatException` (runtime; not wrapped) |
| Layout validation fail at type init | Exception from `NativeLayout.ValidateOrThrow` (type load / first use) |

---

## Exception types allowed in Driver

| Type | Allowed |
|------|---------|
| `ArgumentException` (+ null/out-of-range) | Yes — validation |
| `ObjectDisposedException` | Yes |
| `NativeException` | Yes — handle / marshal / resource misuse |
| `InvalidOperationException` | Not used today |
| Business / HTTP exceptions | **Forbidden** |

---

## `Dispose` special case

`Dispose()` **must not throw**.  
`CloseDevice` failures during dispose are swallowed (best-effort). Prefer explicit `Close()` when status matters.

---

## What Driver must not do

- Throw on `STAT_CMD_TAG_NO_RESP` / timeout / inventory stop
- Catch SDK failures and convert to success
- Log exceptions
- Retry after exception or failed status
