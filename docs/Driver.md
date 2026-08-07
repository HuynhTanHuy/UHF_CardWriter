# Driver — UhfPrimeDriver

Behavioral contract for the thin native Driver API (`UhfPrimeDriver`).
Living document — phase reports archived under docs/archive/.

**Assembly:** `CareHR.UhfCardWriter.Sdk`  
**Source of truth:** `Sdk/Driver/UhfPrimeDriver.cs`  
Driver does **not** implement inventory loops, retry, verify, logging, HTTP, or business rules.

---

## Common rules

| Rule | Behavior |
|------|----------|
| Handle | Private `IntPtr`; never returned to caller |
| Open state | At most one open handle per instance |
| Dispose | Idempotent; best-effort `CloseDevice`; does not throw |
| Thread safety | **Not** thread-safe |
| Parameter errors | `ArgumentException` / null / out-of-range — **no SDK call** |
| Invalid / closed handle | `NativeException` or `ObjectDisposedException` |
| SDK operational errors | Returned as `NativeResult` / `NativeResult<T>` — **not thrown** |

**Legend:** SDK status = integer `STAT_*` from `UHFPrimeReader.dll`. Success ⇔ `NativeResult.Success` ⇔ `StatusCode == 0`.

---

## API summary

### Open / close / discovery

| API | Purpose | Key inputs | Notes |
|-----|---------|------------|-------|
| `IsOpen` | Owns open handle? | — | Does not query hardware |
| `OpenDevice(comPort, baudRate)` | Serial open | port non-empty; baud > 0 | No inventory / RF config |
| `OpenHid(index)` | USB HID open | USB index | Does not enumerate (use GetHidUsb*) |
| `OpenNet(ip, port, timeoutMs)` | TCP open | ip; port ≠ 0; timeout ≥ 0 | No inventory |
| `Close()` | Close + clear handle | — | Handle cleared even if SDK status fails; OK if already closed |
| `GetHidUsbCount()` | USB device count | — | No open handle required |
| `GetHidUsbInfo(index, capacity)` | USB info string | capacity > 0 | `StringBuilder` → managed string |
| `Dispose()` | Best-effort close | — | Never throws |

Already-open Open* → `NativeException`. Disposed → `ObjectDisposedException`.

### Inventory / select

| API | Purpose | Does **not** |
|-----|---------|--------------|
| `InventoryContinue(invCount, invParam)` | Start/continue inventory (**single** call) | Loop / poll for tags |
| `InventoryStop(timeoutMs)` | Stop inventory (single call) | Retry |
| `GetTagUii(timeoutMs)` | Poll one tag identity → `TagIdentityNative` | Inventory loop; never exposes `NativeTagInfo` |
| `SetSelectMask(maskPtr, maskBits, mask)` | Gen2 select mask | Write tag memory |

### Write / read / response

| API | Purpose | Key validation | Does **not** |
|-----|---------|----------------|--------------|
| `WriteTag(option, password, memBank, wordPtr, writeData)` | Single WriteTag | Password 4 bytes; memBank 0..3; writeData non-empty **even** length; word count ≤ 255 | `GetTagResp`, verify, inventory stop |
| `GetTagResp(cmd, timeoutMs)` | Poll access response → `TagResponseNative` | Handle open | Write/retry |
| `ReadTag(option, password, memBank, wordPtr, wordCount)` | Single ReadTag | Password 4 bytes; memBank 0..3; wordCount > 0 | `GetReadTagResp` |
| `GetReadTagResp(timeoutMs, maxDataBytes)` | Poll read + payload → `TagReadNative` | maxDataBytes > 0 | Verify against expected EPC |

### Lock / kill

| API | Purpose | Key validation | Does **not** |
|-----|---------|----------------|--------------|
| `LockTag(password, area, action)` | Single LockTag | Password 4 bytes | `GetTagResp` |
| `KillTag(password)` | Single KillTag | Password 4 bytes | `GetTagResp` |

---

## Exception policy

| Outcome | Mechanism |
|---------|-----------|
| Expected SDK / device / tag outcomes | **Return** `NativeResult` / `NativeResult<T>` |
| Programmer misuse / resource / interop hard failure | **Throw** |

`STAT_*` codes are **not** exceptions.

**Throw when:** disposed; operation needs open handle; open while already open; invalid args; marshal mapping failure after OK status → `NativeException`; DLL missing / BadImage / entry point (CLR, not wrapped); layout validation at type init.

**Allowed types:** `Argument*`, `ObjectDisposedException`, `NativeException`. Business/HTTP exceptions **forbidden**.

**Dispose** must not throw (swallow close failures). Prefer explicit `Close()` when status matters.

Driver must not: throw on tag timeout / no-resp; convert SDK failure to success; log; retry.

---

## Marshal guidelines (key rules)

All native→managed conversion happens **inside Driver**. Native structs stay `internal`.

| Native | Public Driver | Strategy |
|--------|---------------|----------|
| Handle `int64_t` | *(not exposed)* | Owned `IntPtr` |
| Status `int` | `NativeResult.StatusCode` | Return value |
| `char*` COM/IP | `string` | LPStr / Ansi |
| USB info `char*` | `string` | Pre-sized `StringBuilder` |
| Buffers | `byte[]` copy | Pin for call |
| `TagInfo` / `TagResp` | `TagIdentityNative` / `TagResponseNative` | `out` struct → field copy |
| Read payload | `TagReadNative.Data` | Buffer + `ToArray` |

**Layout:** `LayoutKind.Sequential`; `ByValArray` + `SizeConst`; validate via `NativeLayout.ValidateOrThrow` (TagInfo 266; TagResp 262; DevicePara 26).

**P/Invoke:** `CallingConvention.Winapi`; `CharSet.Ansi` for strings; `ExactSpelling=true`; `SetLastError=false`.

**Forbidden publicly:** expose handle / `NativeTag*`; return pinned aliases without copy; change layout without updating `NativeLayout` + SDK report.

Marshal failure → `NativeException` with inner — **not** a fake `NativeResult` status.

---

## Native buffer policy

Current practice: managed `byte[]` / `StringBuilder` only. No `AllocHGlobal` in Driver. `AllowUnsafeBlocks` = false.

| Scenario | Strategy |
|----------|----------|
| Password / mask / write IN | `byte[]` + `[In]` |
| Read data OUT | `NativeBuffer` → `byte[]` copy via `ToArray` |
| USB info string OUT | `StringBuilder` |
| TagInfo / TagResp OUT | `out` struct + map to DTO |
| Long-lived native pointer | Not required — introduce only with ADR |

`NativeBuffer`: Driver-internal OUT wrapper; Dispose nulls reference (no unmanaged free). Prefer returning copied `byte[]` / `TagReadNative` to callers.

Non-goals: buffer pool, custom marshaller, sharing buffers across threads.

---

## Thread safety

**`UhfPrimeDriver` is not thread-safe** (no locks on `_handle`).

| Question | Answer |
|----------|--------|
| Concurrent threads on one instance? | **One** (or externally serialized) |
| Multiple instances? | OK; each owns its handle |
| Share one handle across threads? | **No** |
| Two Drivers on same physical device? | Avoid — SDK-dependent |

**Caller responsibilities:** one Driver per reader session; serialize calls; do not block UI on long SDK timeouts without offloading; do not Dispose while another thread is in a Driver method; inventory loop/stop coordination belongs **above** Driver.

Different Driver instances are generally safe at the managed layer (still subject to device/SDK limits). Reading `IsOpen` during Open/Close on another thread is **not** safe.

See [ADR-004](adr/ADR-004-Thread-Safety.md).

---

## Validation matrix (summary)

Invalid parameters throw **before** any SDK call. Tag APIs: **arguments → RequireHandle → SDK**.

| Area | Rules (throw if violated; SDK not called) |
|------|-------------------------------------------|
| OpenDevice | comPort non-empty; baud > 0; must be closed; not disposed |
| OpenHid / OpenNet | closed + not disposed; OpenNet: ip, port ≠ 0, timeout ≥ 0 |
| Close / GetHidUsb* | not disposed; GetHidUsbInfo capacity > 0 |
| Inventory* / GetTagUii / GetTagResp | handle open |
| SetSelectMask | mask non-null; length ≥ ceil(maskBits/8); handle open |
| WriteTag | password 4 bytes; memBank 0..3; writeData non-null, >0, even; word count ≤ 255; handle open |
| ReadTag | password 4 bytes; memBank 0..3; wordCount > 0; handle open |
| GetReadTagResp | maxDataBytes > 0; handle open |
| LockTag / KillTag | password 4 bytes; handle open |
| NativeBuffer | size > 0; `ToArray` count in range |

**Not validated by Driver:** `option` meaning; Lock `area`/`action`; `GetTagResp` cmd id; EPC business semantics; physical presence (→ `NativeResult` status). `wordPtr` / inventory params are pass-through.

Failed `Open*` leaves `IsOpen == false` (handle stored only when status OK).

---

## Resource lifetime

| State | `_handle` | `IsOpen` |
|-------|-----------|----------|
| New / failed Open / after Close / Dispose | `IntPtr.Zero` | `false` |
| After successful Open* | non-zero | `true` |

**Ownership:** only `UhfPrimeDriver`. Acquire on Open* only if status OK. Release via `Close()` (preferred) or `Dispose()` (best-effort, no throw). Every successful Open must eventually Close or Dispose. No finalizer — callers must `using`/Dispose.

Buffers: caller owns IN arrays; `NativeBuffer` created/disposed inside `GetReadTagResp`; OUT payload in `TagReadNative.Data` is an independent copy.

**Hazard:** `Close()` clears handle even if `CloseDevice` returns error (may orphan native resources if SDK needs retry — documented, not changed).

---

## Related

[SDK.md](SDK.md) · [Architecture.md](Architecture.md) · [ADR-001](adr/ADR-001-Driver-Boundary.md) · [ADR-002](adr/ADR-002-NativeResult.md) · [ADR-003](adr/ADR-003-Handle-Management.md) · [ADR-005](adr/ADR-005-Marshal-Strategy.md) · vendor [SDK_REPORT_UHFPrimeReader.md](SDK_REPORT_UHFPrimeReader.md)
