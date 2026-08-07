# Thread Safety — UhfPrimeDriver

**Phase:** 3  
**Source statement:** Class XML summary: *“Not thread-safe.”*  
**Implementation:** No locks, no `Interlocked`, no synchronized collections on `_handle`.

---

## Verdict

**`UhfPrimeDriver` is not thread-safe.**

---

## Handle vs threads

| Question | Answer |
|----------|--------|
| How many threads may use one instance concurrently? | **One** (or externally serialized to one-at-a-time) |
| How many instances may exist? | Multiple; each owns its own handle |
| May two threads share one HANDLE via one Driver? | **No** — undefined behavior (torn reads/writes of `_handle`, overlapping SDK calls) |
| May two Drivers open the same physical device? | SDK-dependent; Driver does not coordinate — avoid |

---

## Why no lock was added

| Reason | Detail |
|--------|--------|
| Phase 3 scope | Thin native driver; locking is a policy choice for upper layers |
| SDK unknown reentrancy | Vendor DLL thread model not documented as multi-thread safe |
| Inventory pattern | Typically one worker thread polls `GetTagUii` — serialization is natural |
| Premature lock | Can hide caller bugs and deadlock with UI sync context |

See [ADR-004](../adr/ADR-004-Thread-Safety.md).

---

## Caller responsibilities (Phase 4+)

1. Own a single Driver instance per reader session.
2. Serialize all calls on that instance (one worker, or `lock` **above** Driver if multiple threads).
3. Do not call Driver from UI thread for long timeouts without offloading — timeouts are SDK blocking calls.
4. Do not `Dispose` while another thread is inside a Driver method.
5. Inventory loop / stop coordination belongs **above** Driver.

---

## What is safe without synchronization

| Operation | Safe? |
|-----------|-------|
| Concurrent use of **different** `UhfPrimeDriver` instances | Generally yes at managed layer (still subject to device/SDK limits) |
| Concurrent `GetHidUsbCount` on different instances | Yes at managed layer |
| Reading `IsOpen` while another thread Opens/Closes | **Not safe** — may tear / race |

---

## Smoke / tests

Phase 3 smoke is single-threaded by design. Multi-thread stress is out of scope until Phase 4+ explicitly requires it.
