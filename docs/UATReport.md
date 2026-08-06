# UAT Report — CareHR UHF Card Writer

**Phase:** 7D  
**Status:** **NOT EXECUTED / NOT SIGNED OFF**

---

## Session

| Field | Value |
|-------|-------|
| Operator | _(pending — no hardware session on audit host)_ |
| Test date | 2026-08-06 (environment audit only) |
| Build | Release `win-x64` |
| Station OS | Windows 10 Home 25H2 (Build 26200) |
| Reader model | **N/A — not attached** |
| Firmware | **N/A** |
| Reader serial | **N/A** |
| Connection mode | Intended: UsbHid (`appsettings`) |
| API | `http://localhost:5000` — **unreachable** |
| Bearer token | **not configured** |

---

## Checklist results

Copied from [`UAT.md`](UAT.md). All items **Blocked** on this host (no reader / no API).

### Reader

| Item | Result |
|------|--------|
| App starts | Not operator-run in this session (build exists) |
| Refresh lists desk reader | **Fail / Blocked** — USB count 0 |
| Connect → CONNECTED | Blocked |
| Disconnect → READY | Blocked |
| Reconnect | Blocked |

### Scan / Write / Register / UX

| Area | Result |
|------|--------|
| Scan no/one/two tags | Blocked |
| Write → Verify → Register → Completed | Blocked |
| Cancel / errors / busy / shortcuts | Blocked (requires live session) |

---

## Operator confirmation

| Question | Answer |
|----------|--------|
| UI easy to use? | Not assessed |
| Messages clear? | Not assessed |
| Workflow reasonable? | Not assessed |

---

## Overall UAT result

**FAIL — prerequisites not met; no operator sign-off.**

---

## Signature

| Role | Name | Date | Signature / Result |
|------|------|------|--------------------|
| Operator | | | **Pending** |
| Reviewer / QA | Automated audit host | 2026-08-06 | **Not Passed** |

**Notes:** Attach UHF reader, configure API + token, then re-run operator checklist in `UAT.md` and replace this report.
