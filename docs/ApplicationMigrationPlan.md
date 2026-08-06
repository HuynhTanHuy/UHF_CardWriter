# Application Migration Plan — Domain Ports

**Phase:** 6A (plan only — no code)  
**Supersedes / extends:** [ApplicationBoundaryMigrationPlan.md](ApplicationBoundaryMigrationPlan.md)  
**Execute in:** Phase 6B (Application implementation), not 6A

---

## Why migrate

Product is **CareHR card writer** (Business Application). Application currently may still expose interim `IUhf*` / `Tag*` scaffolding. Target language is **Card / Reader / Registration**.

---

## Mapping

| Interim / avoid | Target |
|-----------------|--------|
| `IUhfConnection` | `ICardConnection` |
| `IUhfInventory` | `ICardScanner` |
| `IUhfWriter` | `ICardWriter` |
| `IUhfReader` | `ICardReader` |
| `IUhfTagControl` | `ICardScanner.Select*` + optional `ICardSecurity` |
| `TagIdentity` | `CardIdentity` / `CardInformation` |
| `TagAccessResponse` | `CardWriteResult` |
| `TagReadData` | `CardReadResult` |
| `MemBank` on Application API | **Remove** — Infrastructure default Gen2 EPC write |
| `DeviceResult` / `DeviceErrorCode` | Keep as `OperationResult` basis |

Sdk `IUhf*` **unchanged**.

---

## Ordered steps

1. Implement Application Services + Orchestrator against **new** `ICard*` ports (can introduce ports first).  
2. Add Infrastructure adapters for `ICard*` → `IUhfSdk` / HTTP.  
3. Switch DI registrations to `ICard*`.  
4. Remove interim Application `IUhf*` / `Tag*` types.  
5. Update Infra docs; close boundary debt I-TD-02/03.

---

## Risk & compatibility

| Item | Notes |
|------|-------|
| Risk | Medium rename across Application + Infrastructure |
| Backward compatibility | Big-bang OK while UI still placeholder |
| No change | Native, Driver, SDK Wrapper public contracts |

---

## Explicit non-actions in Phase 6A

- No rename in repo now  
- No service implementation  
- No UI  
- No HTTP client code
