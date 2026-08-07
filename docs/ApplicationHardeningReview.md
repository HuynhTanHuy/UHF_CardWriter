# Application Hardening Review

**Phase:** 9 — Diagnostics & Observability  
**Date:** 2026-08-07  
**Scope:** App host only (no SDK / Driver / Native / Workflow / Backend contract changes)

---

## Diagnostics

| Capability | Status | Location |
|------------|--------|----------|
| File logging (redacted) | Done | `Diagnostics/AppLog.cs` → `%LocalAppData%\CareHR\UhfCardWriter\logs` |
| Operation history + duration | Done | `OperationLogControl` + `AppLog.Operation` |
| Crash handlers | Done | UI / AppDomain / UnobservedTask → crash report |
| Startup config validation | Done | `ConfigurationValidator` + startup dialog |
| Native DLL presence | Done | Checked at composition + Health |
| About / Health / Export | Done | `SupportForm` (Settings button) |
| Secrets redaction | Done | JWT / Bearer never written to log/export |

---

## Maintainability

- Optional `appsettings.Local.json` for machine secrets (example provided).
- Bearer token cleared from committed `appsettings.json`.
- Friendly HTTP/register errors in Infrastructure adapter (no raw body dump to UI).
- Version metadata: `1.0.0+phase9`.

---

## Supportability

- Support dialog: About, Health checks, Open log folder, Export diagnostics bundle.
- Export includes config summary (token redacted), health, timings, operation lines, recent log.
- `docs/SupportGuide.md` for field recovery.

---

## Performance

| Metric | Capture |
|--------|---------|
| Startup | `Program` Stopwatch → session timings |
| Connect / Disconnect / Scan / Write job | Stopwatch in `MainForm` |
| Memory | Included in diagnostics export (WorkingSet / Private) |

No production SLA thresholds enforced — observational only.

---

## Technical Debt

| ID | Item | Severity |
|----|------|----------|
| H-TD-01 | No interactive Settings editor (still edit JSON) | Medium |
| H-TD-02 | Backend “Ready” is config-level (no live ping) | Low |
| H-TD-03 | Reader/SDK firmware version not queried (SDK unchanged) | Low |
| H-TD-04 | Access password still silent-fallback to zeros | Low (warned at startup) |

---

## Gate notes

Production blockers from Phase 8 audit addressed for hardening:

- Global crash handling  
- Durable logs + export  
- Config validation at startup  
- Token not committed / not logged  
- Support surface for IT  

Remaining ops dependency: operator must supply Bearer token via `appsettings.json` or `appsettings.Local.json` before Register succeeds.
