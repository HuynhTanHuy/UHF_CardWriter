# Hardware Validation Report — Phase 7D

**Date:** 2026-08-06  
**Validator role:** Automated environment + discovery audit on development host  
**Product:** CareHR.UhfCardWriter  
**Build audited:** Release `win-x64` App output  

**Result: NOT PASSED** — hardware UAT cannot complete on this host.

---

## Environment

| Item | Observed |
|------|----------|
| OS | Windows 10 Home Single Language 25H2 (Build 26200) / NT 10.0.26200 |
| App RID | `win-x64` |
| `UHFPrimeReader.dll` | Present (203,264 bytes) beside EXE |
| `hidapi.dll` | Present (143,872 bytes) beside EXE |
| App EXE | Present (`CareHR.UhfCardWriter.App.exe`) |
| `appsettings.json` | Present beside EXE |
| CareHR API `http://localhost:5000` | **UNREACHABLE** (timeout) |
| Bearer token | **empty** |

Software stack discovery (`ListUsbReaders` via Application → SDK): **count = 0** (~7 ms). No fake Connect/Write attempted.

---

## Hardware

| Item | Observed |
|------|----------|
| UHF desk reader (PnP / HID name match) | **Not present** |
| Application USB HID enumeration | **0 devices** |
| Serial ports (for COM fallback) | Not used / no UHF confirmed on COM3 |
| Firmware / reader serial / model | **N/A** — device not attached |
| Non-UHF USB “card reader” | Realtek USB 2.0 Card Reader (SD) — **not applicable** |

---

## Performance

| Operation | Measured on this host |
|-----------|------------------------|
| `ListUsbReaders` | ~7–119 ms (empty list) |
| Connect / Inventory / Write / Verify / Register / full job | **Not measured** — blocked by missing reader / API |
| Stress (100–1000 cards) | **Not executed** |
| UI freeze under hardware load | **Not executed** |

---

## Validation matrix (executed vs blocked)

| Area | Status | Notes |
|------|--------|-------|
| Native DLL presence | Pass | Beside Release EXE |
| Software DI / discovery call | Pass | Real SDK path, 0 readers |
| Refresh / Connect / Disconnect / Reinsert | Blocked | No UHF reader |
| RF one/two/no tag / orientation | Blocked | No reader / tags |
| Write / read-back / verify / overwrite | Blocked | No hardware |
| Register 2xx / 401 / 403 / 409 / 500 / timeout | Blocked | API down + empty token |
| Full workflow E2E | Blocked | Prerequisites missing |
| Error recovery (USB yank, API down mid-job) | Partial note | API-down is observable as config/env; USB yank needs hardware |
| Stress | Blocked | — |
| Operator UAT sign-off | Not started | See UATReport |

---

## Defects

| ID | Severity | Layer | Description | Recommendation |
|----|----------|-------|-------------|----------------|
| HV-B-01 | **Blocker** | Hardware | No UHF desk reader enumerated | Attach supported UHF reader; power on; Refresh in App |
| HV-B-02 | **Blocker** | Config / Backend | `Api.BearerToken` empty | Set token in `appsettings.json` for target CareHR env |
| HV-B-03 | **Blocker** | Backend / Environment | `http://localhost:5000` unreachable | Start CareHR API or point BaseUrl to reachable host |
| HV-B-04 | Medium | Config | CardType GUIDs are placeholders | Replace with real CareHR `RfidTagTypeId` values before Register UAT |

No Presentation/Application/SDK/Driver/Native **code defects** were proven on this host; failures above are environment/hardware readiness.

---

## Pass rate

| Category | Pass | Fail / Blocked | N/A |
|----------|------|----------------|-----|
| Environment prerequisites | 4 | 2 | — |
| Hardware device checks | 0 | 1 | Firmware/model |
| RF / Write / Register / Stress / UAT | 0 | All | — |

**Overall Phase 7D: FAIL (blocked)**

---

## Known limitations

1. Mid-job Verify/Register status streaming still limited by single Orchestrator call (documented in 7C) — validate visually during operator UAT when hardware is available.  
2. This report must be re-run on a station with reader + live API before production.

---

## Re-validation trigger

Re-execute Phase 7D when:

- UHF reader lists in App Refresh  
- API BaseUrl responds  
- Bearer token set  
- Operator completes [`UAT.md`](UAT.md) and updates [`UATReport.md`](UATReport.md)
