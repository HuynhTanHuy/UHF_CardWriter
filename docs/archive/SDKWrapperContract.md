# SDK Wrapper Contract

**Phase:** 4  
**Assembly:** `CareHR.UhfCardWriter.Sdk`  
**Entry type:** `UhfPrimeSdk` (`IUhfSdk`)  
**Consumers (later):** Infrastructure adapters only — never call `UhfPrimeDriver` directly.

**Related:** [SDKWrapperDependencyMap.md](SDKWrapperDependencyMap.md), [SDKWrapperReview.md](SDKWrapperReview.md), [DriverContract.md](DriverContract.md)

---

## Common rules

| Rule | Behavior |
|------|----------|
| Native / Driver | Hidden behind Wrapper; no `IntPtr`, no native structs, no `NativeResult` on public API |
| Errors | SDK status → `SdkResult` / `SdkResult<T>`; interop misuse → `SdkException` (or argument exceptions) |
| Business | **Forbidden** (no verify, no EPC rules, no HTTP) |
| Retry / logging / polling loops | **Forbidden** |
| Thread safety | **Not** thread-safe — one SDK instance ↔ one Driver ↔ one reader session; caller serializes |
| State | Owns one `UhfPrimeDriver`; exposes `IsOpen` only (no inventory-running state machine) |

---

## `IUhfSdk`

| Field | Contract |
|-------|----------|
| **Purpose** | Root façade aggregating connection, inventory, write, read, tag-control |
| **Responsibilities** | Own Driver lifetime; expose facet interfaces; Dispose |
| **Non-responsibilities** | Workflows, UI, Application services, retry, logging |
| **Dependencies** | `UhfPrimeDriver` (internal to implementation) |
| **Thread safety** | Not thread-safe |

---

## `IUhfConnection`

| Field | Contract |
|-------|----------|
| **Purpose** | Open/close reader and USB discovery helpers |
| **Responsibilities** | `OpenSerial` / `OpenHid` / `OpenNet` / `Close`; `IsOpen`; USB count/info |
| **Non-responsibilities** | Inventory, tag access, RF power (not on Driver — deferred) |
| **Dependencies** | Driver Open*/Close/GetHidUsb* |
| **Thread safety** | Not thread-safe |

---

## `IUhfInventory`

| Field | Contract |
|-------|----------|
| **Purpose** | Single-call inventory primitives |
| **Responsibilities** | `Start` (= InventoryContinue), `Stop`, `GetCurrentTag` (= GetTagUii once) |
| **Non-responsibilities** | **No poll loop**, no “find single tag”, no multi-tag policy |
| **Dependencies** | Driver InventoryContinue / InventoryStop / GetTagUii |
| **Thread safety** | Not thread-safe |

---

## `IUhfWriter`

| Field | Contract |
|-------|----------|
| **Purpose** | Issue a Gen2 write and fetch its access response |
| **Responsibilities** | `Write` → Driver `WriteTag` then `GetTagResp` (ISO write cmd) when write status OK |
| **Non-responsibilities** | Select mask, verify read-back, inventory stop, password policy beyond length checks (Driver) |
| **Dependencies** | Driver WriteTag, GetTagResp |
| **Thread safety** | Not thread-safe |

---

## `IUhfReader`

| Field | Contract |
|-------|----------|
| **Purpose** | Issue a Gen2 read and fetch payload |
| **Responsibilities** | `Read` → Driver `ReadTag` then `GetReadTagResp` when read status OK |
| **Non-responsibilities** | Compare expected EPC, business verify |
| **Dependencies** | Driver ReadTag, GetReadTagResp |
| **Thread safety** | Not thread-safe |

---

## `IUhfTagControl`

| Field | Contract |
|-------|----------|
| **Purpose** | Select mask, lock, kill — single Driver calls |
| **Responsibilities** | `Select`, `Lock`, `Kill` |
| **Non-responsibilities** | Compose GetTagResp for lock/kill (left to caller if needed); no kill confirmation workflow |
| **Dependencies** | Driver SetSelectMask, LockTag, KillTag |
| **Thread safety** | Not thread-safe |

---

## Out of Phase 4 surface

| Suggested in examples | Decision |
|-----------------------|----------|
| `IUhfPower` | **Deferred** — Driver has no RF power API (removed/not present in Phase 3 Driver). Do not invent without Driver change. |
| `IUhfDevice` / DeviceInformation | **Deferred** — Driver does not expose `GetDevicePara`. |
| Application `IUhfConnection` (Phase 5) | Separate Application abstractions may wrap this SDK later. |
