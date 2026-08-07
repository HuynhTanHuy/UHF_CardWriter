# Driver Contract — UhfPrimeDriver

**Phase:** 3  
**Assembly:** `CareHR.UhfCardWriter.Sdk`  
**Type:** `CareHR.UhfCardWriter.Sdk.Driver.UhfPrimeDriver`  
**Source of truth:** `Sdk/Driver/UhfPrimeDriver.cs`  
**Related:** [ExceptionPolicy.md](ExceptionPolicy.md), [ValidationMatrix.md](ValidationMatrix.md), [DriverApiReview.md](DriverApiReview.md)

This document is the **behavioral contract** of the public Driver API.  
Driver does **not** implement inventory loops, retry, verify, logging, HTTP, or business rules.

**Legend**

| Term | Meaning |
|------|---------|
| SDK status | Integer `STAT_*` from `UHFPrimeReader.dll` |
| Success | `NativeResult.Success == true` ⇔ `StatusCode == 0` |
| Does nothing (caller) | No inventory loop / no retry / no verify performed by Driver |

---

## Common rules

| Rule | Behavior |
|------|----------|
| Handle | Private `IntPtr`; never returned to caller |
| Open state | At most one open handle per instance |
| Dispose | Idempotent; best-effort `CloseDevice`; does not throw |
| Thread safety | **Not** thread-safe (see [ThreadSafety.md](ThreadSafety.md)) |
| Parameter errors | `ArgumentException` / `ArgumentNullException` / `ArgumentOutOfRangeException` — **no SDK call** |
| Invalid / closed handle | `NativeException` or `ObjectDisposedException` |
| SDK operational errors | Returned as `NativeResult` / `NativeResult<T>` — **not thrown** |

---

## `bool IsOpen { get; }`

| Field | Contract |
|-------|----------|
| **Purpose** | Indicate whether this instance currently owns an open reader handle |
| **Input** | None |
| **Output** | `true` if handle ≠ 0 and not disposed |
| **Status** | N/A (property) |
| **Exception** | None |
| **Does nothing** | Does not query hardware |

---

## `NativeResult OpenDevice(string comPort, int baudRate)`

| Field | Contract |
|-------|----------|
| **Purpose** | Open serial (COM) connection |
| **Input** | `comPort` non-empty; `baudRate` > 0 |
| **Output** | `NativeResult`; on success, instance owns handle |
| **Status** | SDK status from `OpenDevice` |
| **Exception** | `ArgumentException` (port); `ArgumentOutOfRangeException` (baud); `NativeException` (already open); `ObjectDisposedException` |
| **Does nothing** | No inventory / no RF config |

---

## `NativeResult OpenHid(ushort index)`

| Field | Contract |
|-------|----------|
| **Purpose** | Open USB HID connection by device index |
| **Input** | `index` (USB enumeration index) |
| **Output** | `NativeResult`; on success, instance owns handle |
| **Status** | SDK status from `OpenHidConnection` |
| **Exception** | `NativeException` (already open); `ObjectDisposedException` |
| **Does nothing** | Does not enumerate devices (use `GetHidUsbCount` / `GetHidUsbInfo`) |

---

## `NativeResult OpenNet(string ip, ushort port, int timeoutMs)`

| Field | Contract |
|-------|----------|
| **Purpose** | Open TCP network connection |
| **Input** | `ip` non-empty; `port` ≠ 0; `timeoutMs` ≥ 0 |
| **Output** | `NativeResult`; on success, instance owns handle |
| **Status** | SDK status from `OpenNetConnection` |
| **Exception** | `ArgumentException` / `ArgumentOutOfRangeException`; `NativeException` (already open); `ObjectDisposedException` |
| **Does nothing** | No inventory |

---

## `NativeResult Close()`

| Field | Contract |
|-------|----------|
| **Purpose** | Close device and release owned handle |
| **Input** | None |
| **Output** | `NativeResult.Ok()` if already closed; else SDK status from `CloseDevice` |
| **Status** | SDK status (or OK if no handle) |
| **Exception** | `ObjectDisposedException` |
| **Does nothing** | Does not retry close |
| **Note** | After call, owned handle is cleared to zero regardless of SDK status (see ResourceLifetime / debt) |

---

## `NativeResult<int> GetHidUsbCount()`

| Field | Contract |
|-------|----------|
| **Purpose** | Return USB HID device count (no open handle required) |
| **Input** | None |
| **Output** | `NativeResult<int>`; `Value` = count when ≥ 0 |
| **Status** | If native return &lt; 0, treated as status; else OK |
| **Exception** | `ObjectDisposedException` |
| **Does nothing** | Does not open a device |

---

## `NativeResult<string> GetHidUsbInfo(ushort index, int capacity = 256)`

| Field | Contract |
|-------|----------|
| **Purpose** | Return USB device info string for index |
| **Input** | `index`; `capacity` > 0 for `StringBuilder` |
| **Output** | `NativeResult<string>` |
| **Status** | SDK status from `CFHid_GetUsbInfo` |
| **Exception** | `ArgumentOutOfRangeException` (capacity); `ObjectDisposedException` |
| **Does nothing** | Does not open a device |

---

## `NativeResult InventoryContinue(byte invCount = 0, uint invParam = 0)`

| Field | Contract |
|-------|----------|
| **Purpose** | Start/continue inventory (**single** SDK call) |
| **Input** | `invCount`, `invParam` as defined by SDK |
| **Output** | `NativeResult` |
| **Status** | SDK status |
| **Exception** | `NativeException` (handle not open); `ObjectDisposedException` |
| **Does nothing** | **No loop**; caller must poll `GetTagUii` / stop |

---

## `NativeResult InventoryStop(ushort timeoutMs = 10000)`

| Field | Contract |
|-------|----------|
| **Purpose** | Stop inventory (**single** SDK call) |
| **Input** | `timeoutMs` |
| **Output** | `NativeResult` |
| **Status** | SDK status |
| **Exception** | `NativeException` (handle not open); `ObjectDisposedException` |
| **Does nothing** | No retry |

---

## `NativeResult<TagIdentityNative> GetTagUii(ushort timeoutMs)`

| Field | Contract |
|-------|----------|
| **Purpose** | Poll one inventory tag identity; marshal to managed type |
| **Input** | `timeoutMs` |
| **Output** | `NativeResult<TagIdentityNative>` (never exposes `NativeTagInfo`) |
| **Status** | SDK status; value only on success |
| **Exception** | `NativeException` (handle / marshal failure); `ObjectDisposedException` |
| **Does nothing** | No inventory loop |

---

## `NativeResult SetSelectMask(ushort maskPtr, byte maskBits, byte[] mask)`

| Field | Contract |
|-------|----------|
| **Purpose** | Set Gen2 select mask |
| **Input** | `mask` non-null; length ≥ ceil(maskBits/8) |
| **Output** | `NativeResult` |
| **Status** | SDK status |
| **Exception** | `ArgumentNullException` / `ArgumentException`; `NativeException` (handle); `ObjectDisposedException` |
| **Does nothing** | Does not write tag memory |

---

## `NativeResult WriteTag(byte option, byte[] accessPassword, byte memBank, ushort wordPtr, byte[] writeData)`

| Field | Contract |
|-------|----------|
| **Purpose** | Issue WriteTag command (**single** call) |
| **Input** | Password exactly 4 bytes; `memBank` 0..3; `writeData` non-empty even length |
| **Output** | `NativeResult` |
| **Status** | SDK status from `WriteTag` |
| **Exception** | Argument* ; `NativeException` (handle); `ObjectDisposedException` |
| **Does nothing** | **No** `GetTagResp`, **no** verify, **no** inventory stop |

---

## `NativeResult<TagResponseNative> GetTagResp(ushort cmd, ushort timeoutMs)`

| Field | Contract |
|-------|----------|
| **Purpose** | Poll access command response; marshal to managed type |
| **Input** | `cmd` (e.g. ISO write/lock/kill); `timeoutMs` |
| **Output** | `NativeResult<TagResponseNative>` |
| **Status** | SDK status |
| **Exception** | `NativeException` (handle / marshal); `ObjectDisposedException` |
| **Does nothing** | No write/retry |

---

## `NativeResult ReadTag(byte option, byte[] accessPassword, byte memBank, ushort wordPtr, byte wordCount)`

| Field | Contract |
|-------|----------|
| **Purpose** | Issue ReadTag command (**single** call) |
| **Input** | Password 4 bytes; `memBank` 0..3; `wordCount` > 0 |
| **Output** | `NativeResult` |
| **Status** | SDK status |
| **Exception** | Argument* ; `NativeException` (handle); `ObjectDisposedException` |
| **Does nothing** | Does not call `GetReadTagResp` |

---

## `NativeResult<TagReadNative> GetReadTagResp(ushort timeoutMs, int maxDataBytes = 512)`

| Field | Contract |
|-------|----------|
| **Purpose** | Poll read response + payload bytes |
| **Input** | `timeoutMs`; `maxDataBytes` > 0 (OUT buffer capacity) |
| **Output** | `NativeResult<TagReadNative>` (response + wordCount + data copy) |
| **Status** | SDK status |
| **Exception** | `ArgumentOutOfRangeException`; `NativeException` (handle / marshal); `ObjectDisposedException` |
| **Does nothing** | No verify against expected EPC |

---

## `NativeResult LockTag(byte[] accessPassword, byte area, byte action)`

| Field | Contract |
|-------|----------|
| **Purpose** | Issue LockTag (**single** call) |
| **Input** | Password 4 bytes; `area`, `action` per SDK |
| **Output** | `NativeResult` |
| **Status** | SDK status |
| **Exception** | Argument* ; `NativeException` (handle); `ObjectDisposedException` |
| **Does nothing** | No `GetTagResp` |

---

## `NativeResult KillTag(byte[] accessPassword)`

| Field | Contract |
|-------|----------|
| **Purpose** | Issue KillTag (**single** call) |
| **Input** | Password 4 bytes |
| **Output** | `NativeResult` |
| **Status** | SDK status |
| **Exception** | Argument* ; `NativeException` (handle); `ObjectDisposedException` |
| **Does nothing** | No `GetTagResp` |

---

## `void Dispose()`

| Field | Contract |
|-------|----------|
| **Purpose** | Release owned handle (best-effort close) |
| **Input** | None |
| **Output** | None |
| **Status** | N/A |
| **Exception** | None (swallows close failures) |
| **Does nothing** | No business cleanup beyond native close |
