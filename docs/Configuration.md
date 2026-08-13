# Configuration

App settings for CareHR UHF Card Writer (API, reader, card, catalogs).
Living document — phase reports archived under docs/archive/.

**Primary file:** `src/CareHR.UhfCardWriter.App/appsettings.json`  
**Secrets override:** `appsettings.Local.json` (from `appsettings.Local.json.example`)  
Settings UI does not edit JSON — change files and restart the app.

---

## Typical keys

### `Api`

| Key | Purpose | Example / default |
|-----|---------|-------------------|
| `BaseUrl` | CareHR API root (HTTPS) | `https://carehr02-mgl-api.2bsolu.com` |
| `CreateRfidCardPath` | Relative create path | `/api/rfid/cards` |
| `DefaultStatus` | Card status on create | `4` (Stock) |
| `DefaultIsActive` | Active flag | `true` |

### `Reader`

| Key | Purpose | Example / default |
|-----|---------|-------------------|
| `DefaultMode` | Connect mode | `UsbHid` |
| `ComPort` | Serial port | `COM3` |
| `BaudRate` | Serial baud | `115200` |
| `NetworkIp` / `NetworkPort` / `NetworkTimeoutMs` | TCP endpoint | `192.168.1.100`, `8080`, `3000` |
| `ScanTimeoutMs` | Scan wait | `3000` |

### `Card`

| Key | Purpose | Example / default |
|-----|---------|-------------------|
| `AccessPasswordHex` | Gen2 access password (8 hex chars) | `00000000` |
| `DefaultBatchCode` | Default batch | `BATCH-001` |
| `EpcEncoding` | EPC string encoding | `Ascii` |
| `SerialPadWidth` | Serial pad width | `8` |

### `Hospitals` / `CardTypes`

Arrays of `{ Id, Name }` (hospitals may also include `Code`). GUIDs must match CareHR catalog for the target environment.

### `Theme` (UI)

Optional accent / status colors (`AccentHex`, `SuccessHex`, `ErrorHex`, `WarningHex`, `NeutralHex`).

---

## JWT / Register authentication

Register, exists-check, and next-serial API calls use a JWT held **in memory only**. The token is **not** stored in `appsettings.json`.

On startup, **LoginForm** authenticates against CareHR:

`POST {Api.BaseUrl}/api/auth/login` with `{ "username", "password" }` → `data.token` → in-memory session.

Restarting Card Writer clears the session; sign in again in the app. Use a hospital-scoped account with RFID access (`RFID_V2`).

---

## Local override

1. Copy `appsettings.Local.json.example` → `appsettings.Local.json` next to the EXE / project.  
2. Optionally override `BaseUrl` / path for non-MGL environments.  
3. Keep Local files out of source control; restart after edits.

Example Local shape:

```json
{
  "Api": {
    "BaseUrl": "https://carehr02-mgl-api.2bsolu.com",
    "CreateRfidCardPath": "/api/rfid/cards",
    "DefaultStatus": 4,
    "DefaultIsActive": true
  }
}
```

---

## Validation & health

- Startup validates critical config (e.g. password hex format warnings).  
- Settings → Health shows config readiness (Native DLL, Backend URL, Auth Session) — **not** a live HTTP ping.  
- Create-card path must remain `/api/rfid/cards` unless Backend changes the contract ([API.md](API.md)).

---

## Related

[SupportGuide.md](SupportGuide.md) · [Operations.md](Operations.md) · [API.md](API.md) · [ReleaseNotes.md](ReleaseNotes.md)
