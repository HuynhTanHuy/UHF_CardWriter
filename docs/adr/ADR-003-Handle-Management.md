# ADR-003: Handle Management

**Status:** Accepted  
**Phase:** 3  
**Date:** 2026-08-06

## Context

SDK exposes `int64_t` connection handles via Open*/CloseDevice. Managed code must not leak handles and must not expose raw pointers to Application/UI.

## Decision

- Store handle as **private `IntPtr`** on `UhfPrimeDriver`
- Implement **`IDisposable`** with best-effort close
- Prefer explicit **`Close()`** when SDK status matters
- Do **not** expose `IntPtr` or `SafeHandle` publicly
- Store handle only when Open status == OK

`SafeHandle` was considered but not used: the value is an SDK opaque id closed via `CloseDevice`, not a Win32 kernel object with a standard release function for `SafeHandle.ReleaseHandle` patterns without wrapping the same call.

## Consequence

- Callers use `using` / `Dispose`
- Double-open throws `NativeException`
- Failed Open leaves `IsOpen == false`

## Alternative Considered

| Alternative | Why rejected |
|-------------|--------------|
| Public `SafeHandle` | Still exposes handle semantics; SDK not a kernel handle |
| Return `IntPtr` to caller | Boundary violation |
| Static global handle | Not multi-instance friendly; hidden lifetime |
