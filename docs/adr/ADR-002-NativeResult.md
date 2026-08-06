# ADR-002: NativeResult

**Status:** Accepted  
**Phase:** 3  
**Date:** 2026-08-06

## Context

SDK returns integer `STAT_*` codes for expected device/tag outcomes (timeout, no tag, bad password). Throwing on every non-zero status would force try/catch for normal control flow and encourage swallowing errors incorrectly.

## Decision

Use **`NativeResult`** / **`NativeResult<T>`** for all completed SDK calls:

- `StatusCode` / `NativeStatus` = raw SDK int
- `Success` = status == 0
- `Message` = descriptive text
- `Value` (generic) = managed payload on success

Do **not** throw for SDK status codes.

## Consequence

- Callers must check `Success` before using `Value`
- Exceptions reserved for validation, disposed, invalid handle, marshal hard failures
- `NativeResult<T>` retained (see `NativeResultReview.md`)

## Alternative Considered

| Alternative | Why rejected |
|-------------|--------------|
| Throw on any non-zero status | Control-flow abuse; poor for inventory poll |
| `(int status, T value)` tuples | Easy to ignore status |
| `bool TryX(out T)` only | Loses SDK code detail |
| Remove `NativeResult<T>` | Forces out-params or weaker typing |
