# Presentation Review — Phase 7

**Date:** 2026-08-06  
**Host:** `CareHR.UhfCardWriter.App`  
**Main surface:** `Forms/MainForm`

---

## Reference UI analysis (CardWritter)

| Aspect | CardWritter | CareHR UHF presentation |
|--------|-------------|---------------------------|
| Layout | Two-column: inputs left, indicator right | Same split; clearer hierarchy + progress + log |
| Actions | Large write buttons | Connect / Scan / Write / Cancel / Refresh / Settings |
| Status | Image + label | StatusPanel with colored states + workflow strip |
| Config | Hardcoded / embedded | `appsettings.json` |
| Coupling | Form ↔ devices/services mixed | Form → Application Services only |

**Improvements:** named controls, keyboard shortcuts, busy disable, operation log, no SDK in UI.

---

## Workflow mapping

| UI action | Application |
|-----------|-------------|
| Connect / Disconnect | `CardConnectionService` |
| Refresh readers | `CardConnectionService.ListUsbReaders` |
| Scan | `CardScanningService.ScanForSingleCard` |
| Write | `CardWriteOrchestrator.RunWriteCardJob` |
| Cancel | `CardWriteOrchestrator.CancelOperation` + CTS |

---

## UX / Accessibility

| Item | Notes |
|------|-------|
| Visual hierarchy | Brand → fields → actions; right side status/progress/result/log |
| Keyboard | F5 Connect, F6 Scan, F7 Write, Esc Cancel, Ctrl+R Refresh |
| Busy state | Inputs/actions disabled; Cancel remains available |
| Contrast | Teal accent on white; status uses strong solid colors |
| Illustration | Text panel (no copied CardWritter bitmap) |

---

## Maintainability

- Custom controls: `StatusPanel`, `WorkflowProgressControl`, `OperationLogControl`
- Config POCOs under `Configuration/`
- Composition: `CompositionRoot` + DI `MainForm`
- Presentation helpers limited to empty/required/number/hex format

---

## Technical debt

| ID | Item |
|----|------|
| P-TD-01 | Settings is informational MessageBox (no live editor) |
| P-TD-02 | No dedicated reader illustration asset yet |
| P-TD-03 | Register-only retry UI not separate (WrittenButUnregistered shown as Error) |
| P-TD-04 | High DPI fine-tuning / accessibility screen-reader labels deferred |

---

## Gate

Presentation is ready for operator UAT with real reader + configured API token.
