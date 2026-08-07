# NativeResult Review

**Phase:** 3 readiness  
**Types:** `NativeResult`, `NativeResult<T>` in `Sdk/Driver/NativeResult.cs`  
**Decision:** **Keep both.** No refactor for Phase 3 gate.

---

## Current design (from source)

### `NativeResult`

| Member | Role |
|--------|------|
| `StatusCode` | Raw SDK `int` |
| `NativeStatus` | Alias of `StatusCode` |
| `Success` | `StatusCode == StatOk (0)` |
| `Message` | Human-readable describe |
| `Ok()` / `FromStatus(int)` | Factories |

Used by: Open*, Close, Inventory*, SetSelectMask, WriteTag, ReadTag, LockTag, KillTag.

### `NativeResult<T>`

| Member | Role |
|--------|------|
| Same status fields | As above |
| `Value` | Managed payload (`T?`); never a native struct |

Used by: `GetHidUsbCount` (`int`), `GetHidUsbInfo` (`string`), `GetTagUii` (`TagIdentityNative`), `GetTagResp` (`TagResponseNative`), `GetReadTagResp` (`TagReadNative`).

---

## Is `NativeResult<T>` needed?

**Yes.**

| Criterion | Assessment |
|-----------|------------|
| APIs with payload | Multiple Driver methods return data only on success |
| Alternative without `T` | Out-params + separate status, or throw on failure — violates Exception Policy |
| Alternative tuple `(NativeResult, T?)` | Weaker typing; easy to ignore status |
| Cost | Small duplicate status fields; acceptable for Phase 3 |

**Value proven:** Call sites get a single return that forces awareness of `Success` before using `Value`, without exceptions for normal SDK failures (timeout, no tag, etc.).

---

## Refactor?

| Proposal | Verdict |
|----------|---------|
| Remove `NativeResult<T>` | **Reject** — loses type-safe payload path |
| Merge into one type with optional object | **Reject** — boxing / weak typing |
| Remove `NativeStatus` alias | Optional cleanup later; **not a blocker**; keep for contract clarity |
| Make `Describe` cover all STAT_* | Documentation debt only; fallback hex exists |

**Phase 3 action:** None (no behavior change).

---

## Contract for Phase 4 consumers

1. Always check `Success` (or `StatusCode`) before reading `Value`.
2. Treat `Value` as undefined when `!Success`.
3. Do not throw on non-success `NativeResult` unless Application policy requires it **above** Driver.
