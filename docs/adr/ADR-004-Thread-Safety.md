# ADR-004: Thread Safety

**Status:** Accepted  
**Phase:** 3  
**Date:** 2026-08-06

## Context

Reader operations are blocking and stateful (inventory continue/stop, select mask, access commands). The vendor DLL does not document multi-threaded use of one handle. Phase 3 Driver must remain thin.

## Decision

**Declare Driver not thread-safe.**  
Do **not** add internal locks in Phase 3.

One Driver instance ↔ one logical session ↔ one thread of execution (or external serialization).

## Consequence

- Phase 4+ must serialize access (dedicated worker recommended)
- Documentation in `ThreadSafety.md` is normative
- Future locking requires a new ADR if product needs concurrent callers

## Alternative Considered

| Alternative | Why rejected |
|-------------|--------------|
| `lock` inside every Driver method | Hides SDK reentrancy unknown; can deadlock with UI |
| Channel/queue inside Driver | Orchestration belongs in Wrapper/Application |
| Thread-safe handle sharing | Unsupported assumption on native DLL |
