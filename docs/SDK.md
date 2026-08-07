# SDK — Wrapper index

Living index for the managed SDK Wrapper and vendor native reference.
Living document — phase reports archived under docs/archive/.

**Assembly:** `CareHR.UhfCardWriter.Sdk`  
**Public entry:** `IUhfSdk` / `UhfPrimeSdk`  
**Consumers:** Infrastructure adapters only — do not call `UhfPrimeDriver` from Application or UI.

Full facet contracts: historical detail in [archive/SDKWrapperContract.md](archive/SDKWrapperContract.md); Driver detail in [Driver.md](Driver.md).  
Vendor CFApi / layout reference: [SDK_REPORT_UHFPrimeReader.md](SDK_REPORT_UHFPrimeReader.md).

---

## Common rules

| Rule | Behavior |
|------|----------|
| Native / Driver | Hidden; no `IntPtr`, native structs, or `NativeResult` on public Wrapper API |
| Errors | SDK status → `SdkResult` / `SdkResult<T>`; interop misuse → `SdkException` (or argument exceptions) |
| Business | **Forbidden** (no verify, EPC rules, HTTP) |
| Retry / logging / poll loops | **Forbidden** |
| Thread safety | **Not** thread-safe — one SDK ↔ one Driver ↔ one reader session; caller serializes |
| State | Owns one `UhfPrimeDriver`; exposes `IsOpen` only |

---

## Public `IUhfSdk` surface

| Facet | Purpose | Responsibilities | Non-responsibilities |
|-------|---------|------------------|----------------------|
| **`IUhfSdk`** | Root façade | Own Driver lifetime; expose facets; Dispose | Workflows, UI, Application services |
| **`IUhfConnection`** | Open/close + USB discovery | `OpenSerial` / `OpenHid` / `OpenNet` / `Close`; `IsOpen`; USB count/info | Inventory, tag access, RF power |
| **`IUhfInventory`** | Single-call inventory | `Start` (= InventoryContinue), `Stop`, `GetCurrentTag` (= GetTagUii once) | Poll loop; “find single tag”; multi-tag policy |
| **`IUhfWriter`** | Gen2 write + access response | `Write` → Driver `WriteTag` then `GetTagResp` when write OK | Select, verify, inventory stop |
| **`IUhfReader`** | Gen2 read + payload | `Read` → Driver `ReadTag` then `GetReadTagResp` when read OK | Compare expected EPC |
| **`IUhfTagControl`** | Select / lock / kill | `Select`, `Lock`, `Kill` | Lock/kill response composition UX; kill confirmation |

---

## Deferred (not on public surface)

| Item | Reason |
|------|--------|
| `IUhfPower` | Driver has no RF power API |
| Device para / `IUhfDevice` | Driver does not expose `GetDevicePara` |

---

## Dependency sketch

```text
IUhfSdk (UhfPrimeSdk)
  └─ UhfPrimeDriver
       └─ UhfPrimeNative (DllImport)
            └─ UHFPrimeReader.dll
```

Application reaches this only via Infrastructure `Card*Adapter`s. See [Infrastructure.md](Infrastructure.md) and [Architecture.md](Architecture.md).
