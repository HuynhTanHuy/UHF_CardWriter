# ADR-001: Driver Boundary

**Status:** Accepted  
**Phase:** 3  
**Date:** 2026-08-06

## Context

UHFPrimeReader is consumed via P/Invoke. Upper layers (Wrapper, Application, UI) must not hold `IntPtr` or native structs. A dedicated boundary is required between raw `DllImport` and future SDK façade.

## Decision

Introduce **`UhfPrimeDriver`** as the only type allowed to call **`UhfPrimeNative`**.

Driver responsibilities:

- Own HANDLE
- Allocate/release managed buffers used for P/Invoke
- Marshal native structs → managed DTOs
- Map SDK status → `NativeResult`

Driver must **not**:

- Implement business rules, retry, verify, inventory loops, logging, HTTP, or UI

## Consequence

- Phase 4 Wrapper composes Driver calls into workflows
- Native layer stays `internal`
- Public surface is managed-only

## Alternative Considered

| Alternative | Why rejected |
|-------------|--------------|
| Call `UhfPrimeNative` from Infrastructure | Leaks IntPtr/structs across layers |
| Merge Driver into Wrapper | Mixes interop with orchestration |
| Expose P/Invoke publicly | Unsafe for App/UI consumers |
