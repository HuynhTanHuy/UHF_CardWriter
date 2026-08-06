# Application Use Cases — CareHR UHF Card Writer

**Phase:** 6A — Use Case Discovery (no code)  
**Product:** CareHR.UhfCardWriter  
**Evidence:** Solution purpose, architecture phases 1–5, Desk Reader EPC write flow, CardWritter registry pattern (`CreateRfidTag`)

---

## UC-001 Connect Reader

| Field | Content |
|-------|---------|
| **Goal** | Establish a working session with a physical UHF desk reader |
| **Actor** | Operator |
| **Trigger** | Operator chooses connection mode (COM / USB HID / Network) and confirms Connect |
| **Precondition** | App running; reader powered; required native DLLs present |
| **Postcondition** | Reader session open; UI shows Connected |
| **Success** | Connection accepted; subsequent scan/write allowed |
| **Failure** | Open failed, no USB device, wrong COM, timeout, SDK unavailable |

---

## UC-002 Disconnect Reader

| Field | Content |
|-------|---------|
| **Goal** | End reader session cleanly |
| **Actor** | Operator |
| **Trigger** | Operator clicks Disconnect / closes app / switches device |
| **Precondition** | Session may be open (idempotent if already closed) |
| **Postcondition** | No open reader session; scan/write disabled |
| **Success** | Close completed (or already closed) |
| **Failure** | Close reports device error (session still cleared at infrastructure policy) |

---

## UC-003 Discover Readers (USB)

| Field | Content |
|-------|---------|
| **Goal** | List available USB HID readers for selection |
| **Actor** | Operator |
| **Trigger** | Open connection dialog / refresh device list |
| **Precondition** | App running (session need not be open) |
| **Postcondition** | Operator sees device count/names |
| **Success** | List returned (may be empty) |
| **Failure** | SDK unavailable |

---

## UC-004 Scan Card

| Field | Content |
|-------|---------|
| **Goal** | Detect exactly one CareHR card in the RF field and obtain its current EPC identity |
| **Actor** | Operator |
| **Trigger** | Operator starts Scan / Write flow requiring a present card |
| **Precondition** | Reader connected; ideally one card in field |
| **Postcondition** | Current card identity known (or failure reported) |
| **Success** | Single card EPC captured |
| **Failure** | No card, timeout, multiple distinct EPCs, reader disconnected, inventory error |

---

## UC-005 Select Card

| Field | Content |
|-------|---------|
| **Goal** | Restrict subsequent access commands to the scanned card (by EPC mask) |
| **Actor** | System (on behalf of Operator write flow) |
| **Trigger** | After successful Scan, before Write |
| **Precondition** | Known current EPC; reader connected |
| **Postcondition** | Select mask applied for following write/read |
| **Success** | Select accepted |
| **Failure** | Select rejected / parameter error / disconnected |

---

## UC-006 Read Card

| Field | Content |
|-------|---------|
| **Goal** | Read EPC (and optionally agreed memory) from the selected/present card |
| **Actor** | Operator or System (verify path) |
| **Trigger** | Explicit Read, or internal step of Verify |
| **Precondition** | Reader connected; card present (select recommended) |
| **Postcondition** | Card content available to Application |
| **Success** | EPC/content read |
| **Failure** | No response, password error, timeout, access denied |

---

## UC-007 Write Card

| Field | Content |
|-------|---------|
| **Goal** | Write the intended CareHR EPC (business identity) onto the physical card |
| **Actor** | Operator |
| **Trigger** | Operator confirms Write with patient/batch context |
| **Precondition** | Connected; card scanned; select applied; target EPC validated by business rules; access password known |
| **Postcondition** | Card memory updated (pending verify) |
| **Success** | Device reports write access success |
| **Failure** | Write fail, password invalid, tag locked, timeout, no tag |

**Note:** Business EPC string rules (hospital/group/id composition) are CareHR rules — not Gen2 bank jargon in the Use Case description.

---

## UC-008 Verify Card

| Field | Content |
|-------|---------|
| **Goal** | Confirm physical card EPC matches the intended business EPC after write |
| **Actor** | System (mandatory before registry) |
| **Trigger** | Automatic after Write success; or Operator re-verify |
| **Precondition** | Intended EPC known; reader connected; card still present |
| **Postcondition** | Verified = true/false recorded for the operation |
| **Success** | Read EPC equals intended EPC |
| **Failure** | Mismatch, read fail, timeout — **must not** register to backend |

---

## UC-009 Register Card

| Field | Content |
|-------|---------|
| **Goal** | Persist card identity in CareHR backend after successful verify |
| **Actor** | System → CareHR API (Operator initiates overall job) |
| **Trigger** | Verify success |
| **Precondition** | Verified EPC; batch/type metadata from Operator input; API reachable (policy) |
| **Postcondition** | Backend holds RFID tag record **or** failure with “already written” reconciliation path |
| **Success** | API 2xx; operation Complete |
| **Failure** | Network/API error, duplicate, auth — card may already be written physically |

**Evidence:** CardWritter `ICardApiClient.CreateRfidTag(epcCode, rfidTagTypeId, rfidTagBatchCode)`.

---

## UC-010 Cancel Operation

| Field | Content |
|-------|---------|
| **Goal** | Stop in-progress scan/write without completing registration |
| **Actor** | Operator |
| **Trigger** | Cancel / Escape during long operation |
| **Precondition** | Operation in progress |
| **Postcondition** | Inventory stopped if needed; UI Idle; no partial registry |
| **Success** | Safe idle |
| **Failure** | Stop inventory timeout (still return to Idle best-effort) |

---

## UC-011 Lock Card (optional / deferred UI)

| Field | Content |
|-------|---------|
| **Goal** | Apply Gen2 lock to protect written EPC/memory |
| **Actor** | Administrator / trained Operator |
| **Trigger** | Explicit Lock action (not part of default write MVP unless product requires) |
| **Precondition** | Connected; card selected; password |
| **Postcondition** | Lock applied or reported failed |
| **Success** | Lock OK |
| **Failure** | Access denied / timeout |

---

## UC-012 Kill Card (optional / high risk)

| Field | Content |
|-------|---------|
| **Goal** | Permanently disable a tag (destructive) |
| **Actor** | Administrator |
| **Trigger** | Explicit Kill with confirmation |
| **Precondition** | Connected; strong confirmation; kill password |
| **Postcondition** | Tag killed or failure |
| **Success** | Kill OK |
| **Failure** | Rejected / timeout |

---

## Out of scope (this product phase)

| Item | Reason |
|------|--------|
| MIFARE block write / Key A/B | Explicitly excluded — UHF Gen2 only |
| Inventory as warehouse portal | This app is card issuance/write, not continuous gate inventory |
| Multi-reader farm | Single desk reader session (per Infra lifetime) |
