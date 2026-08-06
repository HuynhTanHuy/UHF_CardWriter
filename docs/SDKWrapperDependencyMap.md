# SDK Wrapper → Driver Dependency Map

**Phase:** 4  
**Rule:** Every Wrapper method calls **only** `UhfPrimeDriver` (no `UhfPrimeNative`, no DllImport).

---

## Connection (`IUhfConnection`)

| Wrapper API | Driver API |
|-------------|------------|
| `OpenSerial(port, baud)` | `OpenDevice` |
| `OpenHid(index)` | `OpenHid` |
| `OpenNet(ip, port, timeoutMs)` | `OpenNet` |
| `Close()` | `Close` |
| `IsOpen` | `IsOpen` |
| `GetUsbDeviceCount()` | `GetHidUsbCount` |
| `GetUsbDeviceInfo(index, capacity?)` | `GetHidUsbInfo` |

---

## Inventory (`IUhfInventory`)

| Wrapper API | Driver API |
|-------------|------------|
| `Start(invCount?, invParam?)` | `InventoryContinue` |
| `Stop(timeoutMs?)` | `InventoryStop` |
| `GetCurrentTag(timeoutMs)` | `GetTagUii` → map to `TagIdentity` |

---

## Writer (`IUhfWriter`)

| Wrapper API | Driver API |
|-------------|------------|
| `Write(...)` | `WriteTag` |
| *(then, if WriteTag success)* | `GetTagResp(cmd=ISO_WRITE_TAG, timeout)` → map to `TagAccessResponse` |

No inventory stop. No verify.

---

## Reader (`IUhfReader`)

| Wrapper API | Driver API |
|-------------|------------|
| `Read(...)` | `ReadTag` |
| *(then, if ReadTag success)* | `GetReadTagResp` → map to `TagReadData` |

---

## Tag control (`IUhfTagControl`)

| Wrapper API | Driver API |
|-------------|------------|
| `Select(maskPtr, maskBits, mask)` | `SetSelectMask` |
| `Lock(password, area, action)` | `LockTag` |
| `Kill(password)` | `KillTag` |

---

## Result / exception translation

| Driver | Wrapper |
|--------|---------|
| `NativeResult` / `NativeResult<T>` | `SdkResult` / `SdkResult<T>` (copy status fields; remap managed value) |
| `NativeException` | `SdkException` |
| `Argument*` / `ObjectDisposedException` | Pass through (or wrap disposed as `ObjectDisposedException` on SDK type name) |
| `TagIdentityNative` | `TagIdentity` |
| `TagResponseNative` | `TagAccessResponse` |
| `TagReadNative` | `TagReadData` |

---

## Explicit non-mappings

| Concern | Reason |
|---------|--------|
| RF Power | No Driver API |
| DevicePara | No Driver API |
| Inventory poll loop | Forbidden in Wrapper |
| Verify / HTTP | Business / later phases |
