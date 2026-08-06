# Marshal Guideline

**Phase:** 3  
**Native source:** `CFApi.h`, `docs/SDK_REPORT_UHFPrimeReader.md`  
**Implementation:** `Sdk/Native/*` (internal) + `Sdk/Driver/*` (public mapping)

**Rule:** All conversion from native structs/pointers to caller-visible types happens **inside Driver**. Native structs remain `internal`.

---

## Type map

| Native type (CFApi / DLL) | Managed type (Native layer) | Public Driver type | Marshal strategy | Direction | Reason |
|---------------------------|-----------------------------|--------------------|------------------|-----------|---------|
| `int64_t` handle | `IntPtr` | *(not exposed)* | `out IntPtr` / `IntPtr` arg | In/Out | Opaque SDK handle; Driver owns |
| `int` status | `int` | `NativeResult.StatusCode` | Return value | Out | SDK STAT_* |
| `char*` COM / IP | `string` + `LPStr` | `string` | ANSI string marshal | In | CharSet Ansi / ExactSpelling |
| `char*` USB info | `StringBuilder` | `string` (copy) | Pre-sized builder | Out | SDK writes C string |
| `unsigned char*` buffer | `byte[]` `[In]` / `[Out]` | `byte[]` copy | Pin for call | In/Out | Matches sample |
| `unsigned short` | `ushort` | `ushort` | Blittable | In | Size match |
| `unsigned char` | `byte` | `byte` | Blittable | In | Size match |
| `long` timeout (MSVC) | `int` | `int` | 32-bit | In | Windows `long` = 32-bit |
| `TagInfo` | `NativeTagInfo` | `TagIdentityNative` | `out` struct → field copy | Out | Hide layout; copy EPC/CRC/PC |
| `TagResp` | `NativeTagResp` | `TagResponseNative` | `out` struct → field copy | Out | Hide layout |
| Read payload | `byte[]` OUT | `TagReadNative.Data` | Buffer + `ToArray` | Out | Caller owns copy |
| `DevicePara` | `NativeDevicePara` | *(not in Driver API)* | Struct | Out | Phase 2 only; unused by Driver |

---

## Struct layout rules

| Rule | Value |
|------|--------|
| `LayoutKind` | `Sequential` |
| Pack | Default (no explicit Pack) |
| Arrays in struct | `ByValArray` with `SizeConst` |
| Validation | `NativeLayout.ValidateOrThrow()` in `UhfPrimeNative` static ctor |
| Sizes | TagInfo 266; TagResp 262; DevicePara 26 (see SDK report) |

---

## Calling convention

| Setting | Value | Reason |
|---------|-------|--------|
| `CallingConvention` | `Winapi` | Matches SDK C# sample / stdcall on Windows |
| `CharSet` | `Ansi` where strings | `char*` |
| `ExactSpelling` | `true` | Avoid A/W suffix rewriting |
| `SetLastError` | `false` | SDK returns status int, not Win32 last-error |

---

## Direction conventions

| Direction | Pattern |
|-----------|---------|
| In scalar | Managed value type / string |
| In buffer | `[In] byte[]` |
| Out struct | `out NativeXxx` then map |
| Out buffer | Pre-allocated `byte[]` / `NativeBuffer` |
| Out string | `StringBuilder` → `ToString()` |

---

## Forbidden for public API

- Exposing `IntPtr` handle
- Exposing `NativeTagInfo` / `NativeTagResp` / `NativeDevicePara`
- Returning pinned buffer aliases without copy when buffer is reused
- Changing layout without updating `NativeLayout` + SDK report

---

## Marshal failure policy

If mapping throws (unexpected null arrays from marshaller, etc.):

- Wrap as `NativeException` with inner exception
- Do **not** convert to `NativeResult` with fake status
