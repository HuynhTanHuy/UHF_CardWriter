# Tools history (removed before Release)

**Date:** 2026-08-07  
**Action:** Entire `tools/` directory **removed** from the repository.  
**Reason:** No tool served Runtime, Build (solution), Publish, CI/CD, or Production diagnostics after Release. App diagnostics live in the WinForms host (`Settings` → Export / logs under LocalAppData).

---

## Inventory at removal

| Tool | Phase | Purpose | Outcome | Disposition |
|------|-------|---------|---------|-------------|
| Phase3Smoke | 3 | Driver smoke without hardware | Gate passed historically | **REMOVED** (one-time phase smoke) |
| Phase4Smoke | 4 | SDK Wrapper smoke | Gate passed historically | **REMOVED** |
| Phase6DSmoke | 6D | DI resolve + mocked Write→Verify→Register | Useful during development; not product lifecycle | **REMOVED** (phase smoke) |
| Phase7BSmoke | 7B | MainForm create/show/close | Gate passed historically | **REMOVED** |
| Phase7CIntegrationSmoke | 7C | DI + native DLL presence + USB list | Gate passed historically | **REMOVED** |
| Phase8CompatibilitySmoke | 8 | Capture HTTP wire for `POST /api/rfid/cards` | Contract documented in [API.md](../API.md) | **REMOVED** (one-time verification) |
| Phase1RuntimeCheck.ps1 | 1 | Runtime check script | Superseded | **REMOVED** |
| Phase3RuntimeCheck.ps1 | 3 | Invoked Phase3Smoke | Superseded | **REMOVED** |
| StructSizeProbe.cs | 1–3 | Native struct size investigation | Findings captured in Driver/SDK docs + ADRs | **REMOVED** (history in docs, not source) |

---

## Evidence (no KEEP)

| Question | Answer |
|----------|--------|
| In `CareHR.UhfCardWriter.sln`? | **No** |
| Referenced by `src/` ProjectReference? | **No** |
| Required for `dotnet build` solution? | **No** |
| Required for Publish App? | **No** |
| CI/CD pipeline? | **None present** |
| Runtime / UAT operator path? | **No** — operators use the App + [UAT.md](../UAT.md) |
| Production diagnostics? | **In-app** Support/Export (Phase 9), not `tools/` |

---

## Do not restore unless

A maintainer adds a real **tests/** project or packaging/deploy script tied to Release. Do not revive phase-named smoke projects “just in case.”
