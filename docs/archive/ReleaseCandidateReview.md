# Release Candidate Review — Phase 9

**Date:** 2026-08-07  
**Product:** CareHR UHF Card Writer `1.0.0+phase9`

---

## Release blockers

| ID | Item | Status |
|----|------|--------|
| RB-01 | Committed live JWT | **Cleared** (token empty; use Local override) |
| RB-02 | No crash handling | **Fixed** |
| RB-03 | No durable logs / export | **Fixed** |
| RB-04 | No startup config validation | **Fixed** |
| RB-05 | Operator Bearer token for target env | **Ops** — must be set before Register UAT |

No code-level Release Blocker remaining for hardening scope.

---

## Known limitations

1. Settings UI does not edit `appsettings.json` (JSON / Local file only).  
2. Health “Backend Ready” does not HTTP-ping the API (config readiness only).  
3. Reader firmware / SDK version strings not displayed (SDK boundary unchanged).  
4. Hardware UAT and live Register still depend on site token + desk reader.

---

## Technical debt

See `ApplicationHardeningReview.md` (H-TD-01 … H-TD-04).

---

## Future enhancements (out of Phase 9)

- Login flow to obtain Bearer token (no manual paste).  
- Live API health ping.  
- In-app Settings editor.  
- Structured logging (Serilog) if central log shipping required.

---

## RC verdict

**Release Candidate ready** for hardening / supportability, contingent on:

1. Site-specific `Api.BearerToken` via Local config  
2. Hardware available for Connect/Write UAT  
3. CareHR MGL API reachable
