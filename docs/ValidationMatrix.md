# Validation Matrix — UhfPrimeDriver

**Phase:** 3  
**Source:** `Sdk/Driver/UhfPrimeDriver.cs`  
**Policy:** Invalid parameters throw **before** any SDK call (except handle/disposed checks that may precede or follow arg checks as listed).

Validation order used in code for tag APIs with args: **arguments first → RequireHandle → SDK**.

---

## Matrix

| API | Parameter | Rule | Exception if violated | SDK called? |
|-----|-----------|------|----------------------|-------------|
| `OpenDevice` | `comPort` | not null/whitespace | `ArgumentException` | No |
| `OpenDevice` | `baudRate` | > 0 | `ArgumentOutOfRangeException` | No |
| `OpenDevice` | handle state | must be closed | `NativeException` | No |
| `OpenDevice` | disposed | not disposed | `ObjectDisposedException` | No |
| `OpenHid` | handle state | must be closed | `NativeException` | No |
| `OpenHid` | disposed | not disposed | `ObjectDisposedException` | No |
| `OpenNet` | `ip` | not null/whitespace | `ArgumentException` | No |
| `OpenNet` | `port` | ≠ 0 | `ArgumentOutOfRangeException` | No |
| `OpenNet` | `timeoutMs` | ≥ 0 | `ArgumentOutOfRangeException` | No |
| `OpenNet` | handle state | must be closed | `NativeException` | No |
| `Close` | disposed | not disposed | `ObjectDisposedException` | No (if disposed) |
| `GetHidUsbCount` | disposed | not disposed | `ObjectDisposedException` | No |
| `GetHidUsbInfo` | `capacity` | > 0 | `ArgumentOutOfRangeException` | No |
| `GetHidUsbInfo` | disposed | not disposed | `ObjectDisposedException` | No |
| `InventoryContinue` | handle | open | `NativeException` | No |
| `InventoryStop` | handle | open | `NativeException` | No |
| `GetTagUii` | handle | open | `NativeException` | No |
| `SetSelectMask` | `mask` | not null | `ArgumentNullException` | No |
| `SetSelectMask` | `mask.Length` | ≥ ceil(`maskBits`/8) | `ArgumentException` | No |
| `SetSelectMask` | handle | open | `NativeException` | No |
| `WriteTag` | `accessPassword` | not null | `ArgumentNullException` | No |
| `WriteTag` | `accessPassword.Length` | == 4 | `ArgumentException` | No |
| `WriteTag` | `memBank` | 0..3 | `ArgumentOutOfRangeException` | No |
| `WriteTag` | `writeData` | not null | `ArgumentNullException` | No |
| `WriteTag` | `writeData.Length` | > 0 | `ArgumentException` | No |
| `WriteTag` | `writeData.Length` | even | `ArgumentException` | No |
| `WriteTag` | word count | ≤ 255 | `ArgumentOutOfRangeException` | No |
| `WriteTag` | handle | open | `NativeException` | No |
| `GetTagResp` | handle | open | `NativeException` | No |
| `ReadTag` | `accessPassword` | not null / length 4 | `ArgumentNullException` / `ArgumentException` | No |
| `ReadTag` | `memBank` | 0..3 | `ArgumentOutOfRangeException` | No |
| `ReadTag` | `wordCount` | > 0 | `ArgumentOutOfRangeException` | No |
| `ReadTag` | handle | open | `NativeException` | No |
| `GetReadTagResp` | `maxDataBytes` | > 0 | `ArgumentOutOfRangeException` | No |
| `GetReadTagResp` | handle | open | `NativeException` | No |
| `LockTag` | `accessPassword` | not null / length 4 | Argument* | No |
| `LockTag` | handle | open | `NativeException` | No |
| `KillTag` | `accessPassword` | not null / length 4 | Argument* | No |
| `KillTag` | handle | open | `NativeException` | No |
| `NativeBuffer` ctor | `size` | > 0 | `ArgumentOutOfRangeException` | N/A |
| `NativeBuffer.ToArray` | `count` | 0..Length | `ArgumentOutOfRangeException` | N/A |

---

## Intentionally not validated by Driver

| Item | Reason |
|------|--------|
| `option` byte meaning | SDK-defined; business layer decides |
| `area` / `action` for Lock | SDK-defined; no business mapping in Driver |
| `cmd` for `GetTagResp` | Caller supplies ISO command id |
| EPC content / length semantics | Business / Phase 4+ |
| Physical device presence | Returned as SDK status in `NativeResult` |

---

## Notes

- `wordPtr` has no Driver range check (SDK accepts `ushort`; invalid values → SDK status).
- `invCount` / `invParam` not range-checked (pass-through).
- Failed `Open*` leaves `IsOpen == false` (handle stored only when status OK).
