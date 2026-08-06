# Native Buffer Policy

**Phase:** 3  
**Applies to:** `CareHR.UhfCardWriter.Sdk` (Driver + Native P/Invoke)  
**Current practice:** Managed `byte[]` / `StringBuilder` only. No `AllocHGlobal` in Driver.

---

## Goal

Define when each buffer strategy is allowed so Phase 4+ callers and maintainers do not introduce leaks or incorrect pinning.

---

## Mechanisms

### 1. `byte[]`

| | |
|--|--|
| **Use when** | Native signature is `unsigned char*` / `byte*` and CLR default marshaller can pin for the duration of the P/Invoke |
| **Used today** | IN: `SetSelectMask`, `WriteTag`, passwords, `LockTag`, `KillTag`. OUT: `GetReadTagResp` via `NativeBuffer` |
| **Do not use when** | Native keeps the pointer after the call returns; then need unmanaged allocation or pinned GCHandle for lifetime |
| **Lifetime** | Valid for the call; OUT data must be copied before buffer reuse/dispose |

### 2. `StringBuilder`

| | |
|--|--|
| **Use when** | Native writes ANSI/UTF-8 C string into caller buffer (`char*`) |
| **Used today** | `CFHid_GetUsbInfo` → Driver `GetHidUsbInfo` |
| **Do not use when** | Binary payloads, or fixed-size struct fields (`ByValArray`) |
| **Capacity** | Must be pre-sized (`capacity` > 0); Driver validates |

### 3. `Span<byte>` / `Memory<byte>`

| | |
|--|--|
| **Use when** | Future Driver overloads that want zero-copy slicing **and** Native signatures support pinning (`fixed` / `MemoryMarshal`) |
| **Used today** | **Not used** |
| **Do not use when** | Crossing public Driver API without a clear pin strategy; current `DllImport` uses `byte[]` |

### 4. `fixed` (unsafe)

| | |
|--|--|
| **Use when** | Need `byte*` from stack/heap span inside `unsafe` block for a single call |
| **Used today** | **Not used** (`AllowUnsafeBlocks` = false) |
| **Do not use when** | Default marshaller already pins `byte[]` — prefer that for consistency |

### 5. `GCHandle` (Pinned)

| | |
|--|--|
| **Use when** | Native stores pointer beyond the P/Invoke return, or callback needs stable address |
| **Used today** | **Not used** (CFApi inventory is poll-based; no callbacks in scope) |
| **Do not use when** | Short P/Invoke only — unnecessary pin pressure |

### 6. `Marshal.AllocHGlobal` / `FreeHGlobal`

| | |
|--|--|
| **Use when** | Must pass unmanaged memory that outlives the call, or custom layout not covered by marshaller |
| **Used today** | **Not used** |
| **Do not use when** | Current `UhfPrimeNative` signatures accept managed arrays/structs — prefer marshaller |
| **Rule if introduced** | Allocate → try/finally Free; never expose raw `IntPtr` outside Driver |

### 7. `stackalloc`

| | |
|--|--|
| **Use when** | Tiny temporary buffers in `unsafe` local scope |
| **Used today** | **Not used** |
| **Do not use when** | Size depends on tag/read length or must be passed through standard `DllImport` without unsafe |

---

## `NativeBuffer` class

| Rule | Detail |
|------|--------|
| Role | Owned managed OUT buffer wrapper for Driver-internal use (e.g. `GetReadTagResp`) |
| Storage | `byte[]` |
| Dispose | Nulls reference; does **not** free unmanaged memory (none allocated) |
| Public surface | Public type; Phase 4 should prefer Driver methods that return copied `byte[]` / `TagReadNative`, not hold `NativeBuffer` |

---

## Decision matrix (current Phase 3)

| Scenario | Strategy |
|----------|----------|
| Password / mask / write IN | `byte[]` + `[In]` |
| Read data OUT | `NativeBuffer` → `byte[]` → copy via `ToArray` |
| USB info string OUT | `StringBuilder` |
| TagInfo / TagResp OUT | `out` struct + marshal to managed DTO |
| Long-lived native pointer | Not required — do not introduce without ADR |

---

## Non-goals

- No buffer pool
- No custom marshaller
- No sharing buffers across threads
