# Support Guide — CareHR UHF Card Writer

**Audience:** Hospital IT / field support  
**Log folder:** `%LocalAppData%\CareHR\UhfCardWriter\logs`  
**Crash reports:** `%LocalAppData%\CareHR\UhfCardWriter\crashes`  
**Exports:** `%LocalAppData%\CareHR\UhfCardWriter\exports`

Open **Settings** in the app for About, Health, Open log folder, and Export diagnostics.

---

## Reader không nhận / Connect fail

1. Confirm USB cable and desk reader power.  
2. Settings → Health: **Native DLL (UHFPrimeReader)** must be OK.  
3. If DLL missing: reinstall / copy `UHFPrimeReader.dll` + `hidapi.dll` next to the EXE (x64).  
4. Refresh readers (Ctrl+R) and select USB device.  
5. For Serial: verify `Reader.ComPort` / baud in `appsettings.json`.  
6. Export diagnostics and attach to the ticket.

---

## API lỗi / không đăng ký được thẻ

1. Health → **Backend URL** and **Backend Token**.  
2. Set `Api.BearerToken` in `appsettings.json` **or** copy `appsettings.Local.json.example` → `appsettings.Local.json` and paste JWT (preferred for secrets).  
3. Confirm `Api.BaseUrl` = CareHR API host (e.g. `https://carehr02-mgl-api.2bsolu.com`).  
4. Path must be `/api/rfid/cards`.  
5. Restart the app after editing config.

---

## Token lỗi (401)

1. Token expired or wrong environment.  
2. Login to CareHR web → copy a fresh Bearer token.  
3. Update `Api.BearerToken` (Local override recommended).  
4. Retry Write/Register.  
5. Never paste tokens into chat/tickets without redaction; use Export (token shows as “(set)”).

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
2. Common causes: empty token, network, duplicate card number, wrong hospital/type.  
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
| 6 | Restart app; Connect → Scan → Write |

---

## Configuration quick reference

| Key | Purpose |
|-----|---------|
| `Api.BaseUrl` | CareHR API root |
| `Api.BearerToken` | JWT (keep out of source control via Local file) |
| `Api.CreateRfidCardPath` | `/api/rfid/cards` |
| `Hospitals[].Id` | GUID hospital |
| `CardTypes[].Id` | GUID card type |
| `Card.DefaultBatchCode` | Batch code default |
| `Reader.*` | Connect defaults |

Workflow is always: **Connect → Scan → Write → Verify → Register**. Do not skip Verify.
