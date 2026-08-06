# Driver API Review

**Phase:** 3 readiness  
**Type:** `UhfPrimeDriver`  
**Dependencies (all methods):** `UhfPrimeNative` only (no HTTP, no UI, no Application).

**Thread safety (all methods):** **Not thread-safe.** Single-threaded use per instance unless caller external-serializes. See [ThreadSafety.md](ThreadSafety.md).

---

## Connection

### `OpenDevice`

| Field | Detail |
|-------|--------|
| Purpose | Open COM reader |
| Input | `comPort`, `baudRate` |
| Output | `NativeResult` |
| Return status | SDK `OpenDevice` |
| Exception | Argument*; `NativeException` if already open; `ObjectDisposedException` |
| Dependency | `UhfPrimeNative.OpenDevice` |

### `OpenHid`

| Field | Detail |
|-------|--------|
| Purpose | Open USB HID by index |
| Input | `index` |
| Output | `NativeResult` |
| Return status | SDK `OpenHidConnection` |
| Exception | `NativeException` if already open; `ObjectDisposedException` |
| Dependency | `UhfPrimeNative.OpenHidConnection` |

### `OpenNet`

| Field | Detail |
|-------|--------|
| Purpose | Open TCP reader |
| Input | `ip`, `port`, `timeoutMs` |
| Output | `NativeResult` |
| Return status | SDK `OpenNetConnection` |
| Exception | Argument*; `NativeException` if already open; `ObjectDisposedException` |
| Dependency | `UhfPrimeNative.OpenNetConnection` |

### `Close`

| Field | Detail |
|-------|--------|
| Purpose | Close and clear handle |
| Input | — |
| Output | `NativeResult` |
| Return status | OK if no handle; else SDK `CloseDevice` |
| Exception | `ObjectDisposedException` |
| Dependency | `UhfPrimeNative.CloseDevice` |

### `GetHidUsbCount` / `GetHidUsbInfo`

| Field | Detail |
|-------|--------|
| Purpose | Enumerate USB HID devices (no handle) |
| Input | index/capacity for info |
| Output | `NativeResult<int>` / `NativeResult<string>` |
| Return status | Count≥0 → OK; else status / SDK status |
| Exception | Argument* (capacity); `ObjectDisposedException` |
| Dependency | `CFHid_GetUsbCount` / `CFHid_GetUsbInfo` |

---

## Inventory

### `InventoryContinue`

| Field | Detail |
|-------|--------|
| Purpose | Single continue call |
| Input | `invCount`, `invParam` |
| Output | `NativeResult` |
| Return status | SDK |
| Exception | `NativeException` if closed |
| Dependency | `InventoryContinue` |

### `InventoryStop`

| Field | Detail |
|-------|--------|
| Purpose | Single stop call |
| Input | `timeoutMs` (default 10000) |
| Output | `NativeResult` |
| Return status | SDK |
| Exception | `NativeException` if closed |
| Dependency | `InventoryStop` |

### `GetTagUii`

| Field | Detail |
|-------|--------|
| Purpose | Poll one tag UII → managed identity |
| Input | `timeoutMs` |
| Output | `NativeResult<TagIdentityNative>` |
| Return status | SDK |
| Exception | `NativeException` (handle/marshal) |
| Dependency | `GetTagUii` + `MapTagInfo` |

---

## Tag access

### `SetSelectMask`

| Field | Detail |
|-------|--------|
| Purpose | Set select mask |
| Input | `maskPtr`, `maskBits`, `mask` |
| Output | `NativeResult` |
| Return status | SDK |
| Exception | Argument*; `NativeException` if closed |
| Dependency | `SetSelectMask` |

### `WriteTag`

| Field | Detail |
|-------|--------|
| Purpose | Issue write (no verify) |
| Input | `option`, password[4], `memBank` 0..3, `wordPtr`, even `writeData` |
| Output | `NativeResult` |
| Return status | SDK |
| Exception | Argument*; `NativeException` if closed |
| Dependency | `WriteTag` |

### `GetTagResp`

| Field | Detail |
|-------|--------|
| Purpose | Poll access response |
| Input | `cmd`, `timeoutMs` |
| Output | `NativeResult<TagResponseNative>` |
| Return status | SDK |
| Exception | `NativeException` (handle/marshal) |
| Dependency | `GetTagResp` + `MapTagResp` |

### `ReadTag`

| Field | Detail |
|-------|--------|
| Purpose | Issue read |
| Input | password, bank, wordPtr, wordCount>0 |
| Output | `NativeResult` |
| Return status | SDK |
| Exception | Argument*; `NativeException` if closed |
| Dependency | `ReadTag` |

### `GetReadTagResp`

| Field | Detail |
|-------|--------|
| Purpose | Poll read payload |
| Input | `timeoutMs`, `maxDataBytes` |
| Output | `NativeResult<TagReadNative>` |
| Return status | SDK |
| Exception | Argument*; `NativeException` (handle/marshal) |
| Dependency | `GetReadTagResp` + `NativeBuffer` |

### `LockTag` / `KillTag`

| Field | Detail |
|-------|--------|
| Purpose | Issue lock/kill |
| Input | password (+ area/action for lock) |
| Output | `NativeResult` |
| Return status | SDK |
| Exception | Argument*; `NativeException` if closed |
| Dependency | `LockTag` / `KillTag` |

---

## Lifetime

### `Dispose`

| Field | Detail |
|-------|--------|
| Purpose | Best-effort release |
| Input | — |
| Output | void |
| Return status | N/A |
| Exception | None |
| Dependency | `CloseDevice` (swallowed errors) |

---

## API surface review verdict

| Check | Result |
|-------|--------|
| Matches Phase 3 low-level list | Yes (+ HID helpers for OpenHid) |
| No business / retry / verify | Yes |
| No IntPtr / native struct exposure | Yes |
| Ready for Phase 4 wrapper | Yes — wrapper may compose sequences only |
