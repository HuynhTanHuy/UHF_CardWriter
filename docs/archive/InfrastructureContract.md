# Infrastructure Contract

**Phase:** 5  
**Assembly:** `CareHR.UhfCardWriter.Infrastructure`  
**Related:** [InfrastructureDependencyMap.md](InfrastructureDependencyMap.md), [InfrastructureLifetime.md](InfrastructureLifetime.md), [InfrastructureReview.md](InfrastructureReview.md)

---

## Common rules

| Rule | Behavior |
|------|----------|
| SDK | Adapters call **only** `IUhfSdk` facets |
| Application | Implements Application ports; returns `DeviceResult` / `DeviceErrorCode` |
| Forbidden | Business, retry, logging, verify, inventory poll loops, DllImport |
| Thread safety | **Not** thread-safe |
| Lifetime | Shared singleton `IUhfSdk` (see Lifetime doc) |

---

## `UhfConnectionAdapter` → `Application.Abstractions.IUhfConnection`

| Field | Contract |
|-------|----------|
| **Responsibilities** | OpenSerial/OpenHid/OpenNet/Close; IsOpen; USB count/info; map to `DeviceResult` |
| **Non-responsibilities** | Inventory, write/read, RF power, reconnect policy |
| **Dependencies** | `IUhfSdk.Connection` |
| **Lifetime** | Singleton (with SDK) |
| **Thread safety** | Not thread-safe |

---

## `UhfInventoryAdapter` → `IUhfInventory`

| Field | Contract |
|-------|----------|
| **Responsibilities** | Start / Stop / GetCurrentTag (single calls); map `TagIdentity` |
| **Non-responsibilities** | Poll until tag found; multi-tag policy |
| **Dependencies** | `IUhfSdk.Inventory` |
| **Lifetime** | Singleton |
| **Thread safety** | Not thread-safe |

---

## `UhfWriterAdapter` → `IUhfWriter`

| Field | Contract |
|-------|----------|
| **Responsibilities** | Forward Write; map `TagAccessResponse`; map errors via `DeviceErrorCode` |
| **Non-responsibilities** | Select, verify, InventoryStop, retry |
| **Dependencies** | `IUhfSdk.Writer` |
| **Lifetime** | Singleton |
| **Thread safety** | Not thread-safe |

---

## `UhfReaderAdapter` → `IUhfReader`

| Field | Contract |
|-------|----------|
| **Responsibilities** | Forward Read; map `TagReadData` |
| **Non-responsibilities** | Compare expected EPC / verify |
| **Dependencies** | `IUhfSdk.Reader` |
| **Lifetime** | Singleton |
| **Thread safety** | Not thread-safe |

---

## `UhfTagControlAdapter` → `IUhfTagControl`

| Field | Contract |
|-------|----------|
| **Responsibilities** | Select / Lock / Kill |
| **Non-responsibilities** | GetTagResp composition for lock/kill; kill confirmation UX |
| **Dependencies** | `IUhfSdk.TagControl` |
| **Lifetime** | Singleton |
| **Thread safety** | Not thread-safe |

---

## Exception contract

| Source | Adapter behavior |
|--------|------------------|
| Vendor status on `SdkResult` | `DeviceResult` + `DeviceErrorCode` (no throw) |
| `SdkException` | `DeviceException` |
| `Argument*` | Pass through |
| `ObjectDisposedException` | Pass through |

---

## Mapping helpers (internal)

| Type | Role |
|------|------|
| `SdkMapping` | Sdk models/results → Application Devices.* |
| `DeviceErrorMapper` | Vendor status int → `DeviceErrorCode` |
| `DeviceExceptionTranslator` | `SdkException` → `DeviceException` |
