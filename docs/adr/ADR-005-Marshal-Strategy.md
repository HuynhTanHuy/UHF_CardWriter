# ADR-005: Marshal Strategy

**Status:** Accepted  
**Phase:** 3  
**Date:** 2026-08-06

## Context

CFApi structs (`TagInfo`, `TagResp`) and buffers must cross the managed/native boundary. Incorrect layout or exposing native types would couple all layers to CFApi.h packing.

## Decision

1. Keep native structs/`DllImport` **`internal`** in `Sdk/Native`
2. Validate layout at `UhfPrimeNative` static construction (`NativeLayout`)
3. Marshal **inside Driver** to public DTOs (`TagIdentityNative`, `TagResponseNative`, `TagReadNative`)
4. Prefer CLR marshaller with `byte[]` / `StringBuilder` / `out struct`
5. Avoid `AllocHGlobal` unless a future signature requires post-call pointer lifetime
6. CallingConvention `Winapi`, strings ANSI, `ExactSpelling=true`, `SetLastError=false`

## Consequence

- Public API is managed-only
- Layout regressions fail fast at first native touch
- Buffer policy documented in `NativeBufferPolicy.md` / `MarshalGuideline.md`

## Alternative Considered

| Alternative | Why rejected |
|-------------|--------------|
| Public native structs | Boundary leak |
| Manual `AllocHGlobal` for all calls | Complexity/leak risk without benefit |
| `LibraryImport` source generators only | Optional later; current `DllImport` matches verified Phase 2 |
| Unsafe `fixed` everywhere | Project disables unsafe; unnecessary today |
