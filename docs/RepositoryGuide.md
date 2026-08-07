# Repository Guide — CareHR UHF Card Writer

How this repo is organized and how documentation is maintained.
Living document — phase reports archived under docs/archive/.

---

## Structure

```text
UHF_CardWriter/
  CareHR.UhfCardWriter.sln
  Directory.Build.props
  src/
    CareHR.UhfCardWriter.App            # WinForms host, UI, config, diagnostics
    CareHR.UhfCardWriter.Application    # Use cases, services, ICard* ports, DTOs
    CareHR.UhfCardWriter.Infrastructure # Card* adapters, HTTP registrar, DI
    CareHR.UhfCardWriter.Sdk            # IUhfSdk, Driver, Native P/Invoke
  docs/                                 # Living docs + ADRs + archive
```

Layering overview: [Architecture.md](Architecture.md).

**Note:** The former `tools/` tree (phase smokes / probes) was **removed** before Release. History: [archive/ToolsHistory.md](archive/ToolsHistory.md). Product diagnostics are in-app (Settings → Health / Export).

---

## Documentation strategy

| Kind | Location | Role |
|------|----------|------|
| **Living docs** | `docs/*.md` (consolidated) | Current contracts and ops — keep accurate as code changes |
| **ADRs** | `docs/adr/` | Durable decisions (driver boundary, NativeResult, handle, threads, marshal) |
| **Field support** | `SupportGuide.md` | Troubleshooting for hospital IT |
| **Archive** | `docs/archive/` | Phase gate reports, migration plans, one-off reviews |

### Living set (start here)

| File | Topic |
|------|-------|
| [Architecture.md](Architecture.md) | Layers + doc map |
| [Application.md](Application.md) | Domain, UCs, BRs, workflow, DI |
| [Driver.md](Driver.md) | Driver contract |
| [Infrastructure.md](Infrastructure.md) | Adapters + lifetime |
| [SDK.md](SDK.md) | `IUhfSdk` index |
| [API.md](API.md) | CareHR create-card HTTP |
| [Configuration.md](Configuration.md) | appsettings |
| [Operations.md](Operations.md) | Ops + log paths |
| [ReleaseNotes.md](ReleaseNotes.md) | RC status |
| [RepositoryGuide.md](RepositoryGuide.md) | This file |
| [SupportGuide.md](SupportGuide.md) | Full troubleshooting |
| [SDK_REPORT_UHFPrimeReader.md](SDK_REPORT_UHFPrimeReader.md) | Vendor native reference |

Phase-numbered discovery/review markdown lives under [docs/archive/](archive/). Living docs above are the source of truth for day-to-day maintenance.

---

## Archive policy

1. **Archive** when content is a completed phase gate, migration plan, or point-in-time review that is no longer the operational contract.  
2. **Keep living** when content defines current behavior (API route, Driver rules, DI graph, config keys).  
3. Prefer **move** over delete; preserve filenames so old links remain findable under `archive/`.  
4. Living docs may omit obsolete “Phase N gate PASS/FAIL” chatter; archive retains that evidence.  
5. Update living docs when code contracts change; do not silently diverge.

---

## Tool policy

There is **no** `tools/` folder in the Release tree.

| Need | Where |
|------|--------|
| Field diagnostics | In-app Settings (About / Health / Export) + LocalAppData logs |
| Historical phase smoke inventory | [archive/ToolsHistory.md](archive/ToolsHistory.md) |
| Automated tests (future) | Add a proper `tests/` project if/when CI requires it — do not revive phase smokes |

---

## Maintenance guidelines

1. Prefer editing the **living** consolidated file over updating every phase report.  
2. When renaming Application ports/adapters (`ICard*` / `Card*Adapter`), update [Application.md](Application.md) and [Infrastructure.md](Infrastructure.md) together.  
3. When CareHR HTTP contract changes, update [API.md](API.md) and [Configuration.md](Configuration.md) first.  
4. Driver behavioral changes require [Driver.md](Driver.md) (+ ADR if policy changes).  
5. Do not commit Bearer tokens; document Local override only.  
6. Keep living files substantial but maintainable (~80–200 lines); link to SupportGuide / SDK report / ADRs for depth.  
7. New ADRs for irreversible policy (threading, multi-reader factories, marshal strategy).

---

## Related

[Architecture.md](Architecture.md) · [ReleaseNotes.md](ReleaseNotes.md) · [SupportGuide.md](SupportGuide.md)
