# CareHR UHF Card Writer

Desktop WinForms application for writing UHF EPC Gen2 RFID cards and registering them with the CareHR backend.

**Status:** Release Candidate (`1.0.0+phase9`) — see [docs/ReleaseNotes.md](docs/ReleaseNotes.md).

---

## Project overview

Operators connect a desk reader, scan a single tag, write an intended EPC identity, verify the read-back, then register the card via CareHR `POST /api/rfid/cards`.

Workflow:

```text
Connect → Scan → Write → Verify → Register → Done
```

---

## Architecture

Clean Architecture layers:

```text
App (WinForms) → Application (ICard* ports) → Infrastructure (adapters + HTTP)
                                         ↘ Sdk (IUhfSdk → Driver → Native DLL)
```

Details: [docs/Architecture.md](docs/Architecture.md).

---

## Folder structure

```text
UHF_CardWriter/
  CareHR.UhfCardWriter.sln
  src/
    CareHR.UhfCardWriter.App
    CareHR.UhfCardWriter.Application
    CareHR.UhfCardWriter.Infrastructure
    CareHR.UhfCardWriter.Sdk
  docs/                 # Living docs + adr/ + archive/
```

---

## Requirements

- Windows x64
- .NET 8 SDK
- UHF desk reader + `UHFPrimeReader.dll` / `hidapi.dll` (copied next to the EXE by build)
- CareHR API access + Bearer token for Register

---

## Configuration

Edit `appsettings.json` next to the EXE, or use `appsettings.Local.json` for secrets:

| Key | Purpose |
|-----|---------|
| `Api.BaseUrl` | CareHR API root |
| `Api.BearerToken` | JWT (prefer Local file) |
| `Api.CreateRfidCardPath` | `/api/rfid/cards` |
| `Hospitals` / `CardTypes` | GUID catalog |
| `Reader.*` / `Card.*` | Device and EPC defaults |

See [docs/Configuration.md](docs/Configuration.md).

---

## Build

```powershell
dotnet build CareHR.UhfCardWriter.sln -c Debug -p:Platform=x64
dotnet build CareHR.UhfCardWriter.sln -c Release -p:Platform=x64
```

---

## Run

```powershell
dotnet run --project src/CareHR.UhfCardWriter.App -c Debug -p:Platform=x64
```

Or launch the EXE under `src/CareHR.UhfCardWriter.App/bin/x64/.../`.

Shortcuts: **F5** Connect, **F6** Scan, **F7** Write, **Esc** Cancel.

---

## Deployment

1. Publish/copy the App `win-x64` output folder (includes native DLLs).  
2. Place site-specific `appsettings.json` / `appsettings.Local.json`.  
3. Confirm Health (Settings) shows Native DLL + config readiness.  
4. Run UAT checklist: [docs/UAT.md](docs/UAT.md).

---

## RFID workflow

1. Connect reader  
2. Scan for exactly one card  
3. Write intended identity  
4. Verify (mandatory)  
5. Register to CareHR  

Business rules: [docs/Application.md](docs/Application.md).  
HTTP contract: [docs/API.md](docs/API.md).

---

## Documents

Index: [docs/README.md](docs/README.md)  
Repository policy: [docs/RepositoryGuide.md](docs/RepositoryGuide.md)  
Field support: [docs/SupportGuide.md](docs/SupportGuide.md)

Historical phase smoke tools were removed before Release; see [docs/archive/ToolsHistory.md](docs/archive/ToolsHistory.md).
