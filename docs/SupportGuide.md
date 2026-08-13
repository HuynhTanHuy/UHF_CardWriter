# Support Guide — CareHR UHF Card Writer

**Audience:** Hospital IT / field support  
**Log folder:** `%LocalAppData%\CareHR\UhfCardWriter\logs`  
**Crash reports:** `%LocalAppData%\CareHR\UhfCardWriter\crashes`  
**Exports:** `%LocalAppData%\CareHR\UhfCardWriter\exports`

Open **Settings** in the app for About, Health, Open log folder, and Export diagnostics.

---

## Operator workflow

1. Open `CareHR.UhfCardWriter.App.exe`.  
2. Sign in with CareHR username/password (LoginForm).  
3. **Connect** the desk reader.  
4. Confirm RF Power / Out Interface / Volume (buzzer) as needed.  
5. **Start** batch (next serial is resolved from CareHR before start).  
6. Place card → Scan → Write → Verify → Register.

---

## Reader không nhận / Connect fail

1. Confirm USB cable and desk reader power.  
2. Settings → Health: **Native DLL (UHFPrimeReader)** must be OK.  
3. If DLL missing: reinstall / copy `UHFPrimeReader.dll` + `hidapi.dll` next to the EXE (**x86**).  
4. Refresh readers (Ctrl+R) and select USB device.  
5. For Serial: verify `Reader.ComPort` / baud in `appsettings.json`.  
6. Export diagnostics and attach to the ticket.

---

## API lỗi / không đăng ký được thẻ

1. Health → **Backend URL** and **Auth Session**.  
2. Confirm `Api.BaseUrl` = CareHR API host (e.g. `https://carehr02-mgl-api.2bsolu.com`).  
3. Path must be `/api/rfid/cards`.  
4. Sign in again with a hospital-scoped user that has RFID enabled.  
5. Restart the app after editing config.

---

## Token / session lỗi (401)

1. Session expired or wrong environment.  
2. Sign in again in LoginForm (JWT is memory-only; restart always requires login).  
3. Retry Start / Register.  
4. Never paste tokens into chat/tickets; Export shows auth as set/not set only.

---

## Permission lỗi (403)

1. Account lacks RFID access or hospital scope / `RFID_V2` feature.  
2. Do **not** treat 403 as expired token.  
3. Use another CareHR account or enable the hospital feature.

---

## Write thất bại

1. Ensure **Connect** succeeded first.  
2. Only one card in the field; Scan should show a single EPC.  
3. Check access password (`Card.AccessPasswordHex`, 8 hex chars). Startup warns if invalid.  
4. Verify Target EPC / hospital code / serial.  
5. If Verify fails after write: do **not** rewrite blindly — inspect card with Scan and follow hospital policy.

---

## Register thất bại (card already written)

1. Physical card may already be written & verified (`WrittenButUnregistered`).  
2. Common causes: session expired, network, duplicate card number, wrong hospital/type, 403.  
3. Message “Card number already exists…” → use another serial or clear duplicate in CareHR.  
4. Do not change Backend contract; fix config/data then reconcile in CareHR UI if needed.

---

## Recovery checklist

| Step | Action |
|------|--------|
| 1 | Settings → Health — note FAIL rows |
| 2 | Export diagnostics |
| 3 | Open log folder — attach latest `app-*.log` |
| 4 | If crash: attach latest file under `crashes\` |
| 5 | Confirm `appsettings.json` / Local (no secrets in email body) |
| 6 | Restart app; Login → Connect → Start |

---

## Configuration quick reference

| Key | Purpose |
|-----|---------|
| `Api.BaseUrl` | CareHR API root |
| `Api.CreateRfidCardPath` | `/api/rfid/cards` |
| `Hospitals[].Id` | GUID hospital |
| `CardTypes[].Id` | GUID card type |
| `Reader.*` | Connect defaults |

JWT is obtained via LoginForm — **not** stored in config.
