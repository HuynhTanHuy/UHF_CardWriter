# Release Notes — CareHR UHF Card Writer

RC status, limitations, and cleared blockers for field/hardening use.
Living document — phase reports archived under docs/archive/.

**Product:** CareHR UHF Card Writer `1.0.0+phase9`  
**Last reviewed:** 2026-08-07

---

## Verdict

**Release Candidate ready** for hardening / supportability, contingent on site token, desk reader, and reachable CareHR API.

Earlier “not production ready” gate (no hardware/UAT on validation host) remains an **ops/site** constraint — not an open code hardening blocker.

---

## Release blockers (cleared in code)

| ID | Item | Status |
|----|------|--------|
| RB-01 | Committed live JWT | **Cleared** — token empty in shared config; use Local override |
| RB-02 | No crash handling | **Fixed** |
| RB-03 | No durable logs / export | **Fixed** |
| RB-04 | No startup config validation | **Fixed** |
| RB-05 | Operator Bearer token for target env | **Ops** — must be set before Register UAT |

No code-level release blocker remaining for the Phase 9 hardening scope.

---

## Known limitations

1. Settings UI does not edit `appsettings.json` (JSON / Local file only).  
2. Health “Backend Ready” does not HTTP-ping the API (config readiness only).  
3. Reader firmware / SDK version strings not displayed (SDK boundary unchanged).  
4. Hardware UAT and live Register still depend on site token + desk reader.

---

## Site readiness (ops)

Before production operator use on a station:

1. Supported UHF desk reader connected; Connect / Disconnect / Refresh pass.  
2. One-tag write + verify on physical Gen2 card.  
3. Valid `Api.BearerToken` (Local) + correct hospital / card type GUIDs.  
4. CareHR MGL (or target) API reachable over HTTPS.  
5. Operator UAT signed where required by hospital process.

---

## Future enhancements (out of current RC)

- Login flow to obtain Bearer token (no manual paste).  
- Live API health ping.  
- In-app Settings editor.  
- Structured logging (e.g. Serilog) if central log shipping is required.

---

## Related

[SupportGuide.md](SupportGuide.md) · [Operations.md](Operations.md) · [Configuration.md](Configuration.md) · [API.md](API.md) · [UAT.md](UAT.md)
