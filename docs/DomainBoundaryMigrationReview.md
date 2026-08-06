# Domain Boundary Migration Review — Phase 6B

**Date:** 2026-08-06  
**Plan:** [ApplicationMigrationPlan.md](ApplicationMigrationPlan.md)  
**Scope:** Rename Application ports/DTOs to Card domain; rebind Infrastructure adapters. No services, no Sdk/Driver/Native changes.

---

## Migration Summary

| From (Application) | To |
|--------------------|-----|
| `IUhfConnection` | `ICardConnection` |
| `IUhfInventory` | `ICardScanner` (`StartScan` / `StopScan` / `TryGetCard` / `SelectByIdentity`) |
| `IUhfWriter` | `ICardWriter` (`WriteEpc` — no MemBank) |
| `IUhfReader` | `ICardReader` (`ReadEpc` — no MemBank) |
| `IUhfTagControl` | `ICardSecurity` (Lock/Kill); Select moved to scanner |
| `TagIdentity` | `CardIdentity` + `CardInformation` |
| `TagAccessResponse` | `CardWriteResult` |
| `TagReadData` | `CardReadResult` |
| `MemBank` | **Removed** from Application (Infra uses Sdk `MemBank.Epc`, wordPtr=2) |

| Infrastructure | Change |
|----------------|--------|
| `Uhf*Adapter` | Replaced by `Card*Adapter` implementing `ICard*` |
| DI | Registers `ICard*` → `Card*Adapter` |
| Mapping | `SdkMapping` → Card* DTOs + `DeviceErrorCode` |

**Unchanged:** Sdk, Driver, Native, `DeviceResult`, `DeviceErrorCode`, `DeviceException`, business workflow (none implemented yet).

---

## Boundary Review

```text
Application (ICard*, Card*, DeviceResult)
    ↓
Infrastructure (Card*Adapter)
    ↓
SDK (IUhfSdk) — UHF language
    ↓
Driver → Native
```

Application contains **no** `IUhf*`, `Tag*`, `MemBank`, `SdkResult`, `Native*`.

---

## Dependency Review

| Project | References |
|---------|------------|
| Application | none below |
| Infrastructure | Application + Sdk |
| App | Application + Infrastructure |

No reverse dependencies.

---

## Compatibility

| Concern | Status |
|---------|--------|
| Big-bang rename | Accepted (no UI consumers of old ports) |
| Runtime behavior of Sdk/Driver | Unchanged |
| Write/Read surface | Narrowed to EPC helpers (domain intent); Gen2 defaults in Infra |
| `ICardRegistrar` | Not in 6B (no prior port to migrate; Phase 6C+ with services) |

---

## Technical Debt / Known Limitations

| ID | Item | Notes |
|----|------|-------|
| M-TD-01 | `AddUhfInfrastructure` name still says Uhf | Infra DI method name; optional rename later |
| M-TD-02 | `wordCount` still on `ReadEpc` | Needed for variable EPC length; not MemBank |
| M-TD-03 | Lock area/action still raw bytes | Deferred until CardSecurity Use Case shapes them |
| M-TD-04 | No Application Services yet | Phase 6C |

---

## Checklist

- [x] Application business language ports/DTOs  
- [x] No `IUhf*` / `Tag*` / `MemBank` in Application  
- [x] No SdkResult/NativeResult/NativeException on Application  
- [x] Infrastructure maps ICard* → IUhfSdk  
- [x] Sdk/Driver/Native untouched  
- [x] No new Application Services / Use Cases  
