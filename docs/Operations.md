# Operations

Short ops overview for hospital IT / field support.
Living document — phase reports archived under docs/archive/.

For full troubleshooting (reader, API, token, write, register recovery), see **[SupportGuide.md](SupportGuide.md)**.

---

## Paths

| Kind | Location |
|------|----------|
| Logs | `%LocalAppData%\CareHR\UhfCardWriter\logs` (`app-*.log`) |
| Crash reports | `%LocalAppData%\CareHR\UhfCardWriter\crashes` |
| Diagnostics exports | `%LocalAppData%\CareHR\UhfCardWriter\exports` |

In-app: **Settings** → About / Health / Open log folder / Export diagnostics.

---

## Operator workflow (always)

```text
Open EXE → Login → Connect → Start → Scan → Write → Verify → Register
```

Do not skip Verify. On **WrittenButUnregistered**, sign in again / fix network/data and retry register — do not rewrite blindly.

---

## Quick triage

| Symptom | First checks |
|---------|----------------|
| Connect fail | Cable/power; Health → Native DLL; USB Refresh; COM settings |
| API / register fail | Health → Backend URL + Auth Session; BaseUrl + `/api/rfid/cards`; LoginForm |
| 401 | Session expired — login again in the app (memory-only JWT) |
| 403 | Account/hospital RFID permission — do not treat as expired token |
| Write fail | Connected; single card; access password hex; target EPC |
| Register after write | Session/network/duplicate card number/hospital-type mismatch |

Recovery: Health FAIL rows → Export diagnostics → attach latest log (and crash file if any) → confirm config without pasting secrets → restart → Login → Connect → Start.

Native DLLs (`UHFPrimeReader.dll`, `hidapi.dll`) must sit next to the **x86** EXE.

---

## Config & release

- Secrets: [Configuration.md](Configuration.md) (`appsettings.Local.json`).  
- RC / limitations: [ReleaseNotes.md](ReleaseNotes.md).  
- HTTP contract: [API.md](API.md).
