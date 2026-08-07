# Backend Integration Review

**Phase:** 8 (corrected)  
**Date:** 2026-08-07  
**Contract source:** Live CareHR MGL API + frontend create-card curl.

---

## Compatibility

| Check | Status |
|-------|--------|
| `HttpCardRegistrarAdapter` → `POST /api/rfid/cards` | Pass |
| Body matches CareHR `UpsertRFIDCardRequest` (camelCase) | Pass |
| Hospital + card types from MGL catalog in `appsettings.json` | Pass |
| Workflow Write → Verify → Register | Pass |

---

## Security

| Topic | Note |
|-------|------|
| Bearer token | Configured in `appsettings.json` — rotate when expired; avoid committing long-lived secrets to shared remotes |
| HTTPS | Production BaseUrl uses HTTPS |
| Hospital scope | JWT carries `hospitalId`; body also sends `hospitalId` |

---

## Dependency

```text
MainForm (hospital + type + batch)
  → CardWriteOrchestrator
    → CardRegistrationService
      → HttpCardRegistrarAdapter
        → POST {BaseUrl}/api/rfid/cards
```

---

## Technical Debt

| ID | Item | Severity |
|----|------|----------|
| TD-01 | Bearer token in appsettings (ops refresh) | Medium |
| TD-02 | Legacy CardWritter OData client is obsolete | Doc only |
| TD-03 | Sync-over-async HTTP | Low |

---

## Recommendation

1. Keep `/api/rfid/cards` as the only create-card path for UhfCardWriter.  
2. Refresh `Api.BearerToken` when JWT expires.  
3. Prefer operator login flow later instead of static token.
