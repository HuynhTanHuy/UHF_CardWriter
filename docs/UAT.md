# UAT — CareHR UHF Card Writer

**Product:** CareHR.UhfCardWriter  
**Build:** Debug or Release `win-x86` App output  
**Prerequisites:**

1. UHF desk reader powered and connected (USB HID / COM / Network as configured).  
2. `UHFPrimeReader.dll` + `hidapi.dll` next to the EXE (copied by build).  
3. Edit `appsettings.json` beside the EXE:
   - `Api.BaseUrl`
   - `CardTypes[].Id` matching CareHR RFID tag types  
4. CareHR username/password for a hospital-scoped RFID user (LoginForm).  
5. At least one blank/writable Gen2 card in the field for write tests.

---

## Operator Checklist

### Reader

- [ ] App starts without error  
- [ ] **Refresh** lists the desk reader (or Serial/Network entry works)  
- [ ] **Connect (F5)** → status **CONNECTED**  
- [ ] **Disconnect** → status **READY**  
- [ ] Reconnect succeeds  

### Scan

- [ ] **Scan (F6)** with **no card** → clear error / no card message (not crash)  
- [ ] Scan with **one card** → Current EPC shown  
- [ ] Scan with **two cards** → multiple-cards message  
- [ ] **Cancel (Esc)** during scan returns to safe connected/idle  

### Write job (Scan → Write → Verify → Register)

- [ ] Fill Hospital / Card type / Batch / Current # (Target EPC preview OK)  
- [ ] **Write (F7)** with one card → Busy disables inputs  
- [ ] Progress moves toward Done on success  
- [ ] Status **SUCCESS** and log shows business messages only  
- [ ] Current # increments after success (when Start..End allows)  

### Verify / Register outcomes

- [ ] Verify mismatch (if induced) → **ERROR**, no register  
- [ ] Register fail (bad token / API down) → written-but-unregistered style message; card not auto-rewritten  
- [ ] Register success with valid API → Complete  

### Errors & UX

- [ ] Error text is readable (no stack trace / no Native jargon)  
- [ ] UI does not freeze (Busy cursor / controls disabled; Cancel available)  
- [ ] **Settings** shows config summary  
- [ ] Close app disconnects cleanly  

### Regression

- [ ] Keyboard: F5 / F6 / F7 / Esc / Ctrl+R  
- [ ] Restart app, Connect again  

---

## Sign-off

| Role | Name | Date | Result |
|------|------|------|--------|
| Operator | | | Pass / Fail |
| Reviewer | | | Pass / Fail |

**Notes:**
