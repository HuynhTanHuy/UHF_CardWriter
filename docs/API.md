# API — CareHR create-card contract

Production HTTP contract used by `HttpCardRegistrarAdapter`.
Living document — phase reports archived under docs/archive/.

**Authoritative source:** CareHR frontend / live MGL API (not legacy CardWritter OData).

---

## Endpoint

| Item | Value |
|------|--------|
| Method | `POST` |
| Route | `{BaseUrl}/api/rfid/cards` |
| BaseUrl (MGL example) | `https://carehr02-mgl-api.2bsolu.com` |
| Auth | `Authorization: Bearer {token}` (from Writer LoginForm → in-memory session) |
| Accept | `application/json, text/plain, */*` |
| Content-Type | `application/json` |

Config keys: `Api.BaseUrl`, `Api.CreateRfidCardPath` (default `/api/rfid/cards`). JWT is **not** in config — see [Configuration.md](Configuration.md).

Login (Writer): `POST {BaseUrl}/api/auth/login` with `{ "username", "password" }` → `data.token`.

---

## Request body (`UpsertRFIDCardRequest`, camelCase)

| Wire field | Source |
|------------|--------|
| `hospitalId` | Selected hospital / `RegistrationRequest.HospitalId` |
| `rfidCardNumber` | ASCII decode of EPC when printable; else EPC hex |
| `rfidCardTypeId` | Selected card type GUID |
| `rfidCardBatchCode` | Batch text / default `BATCH-001` |
| `status` | `Api.DefaultStatus` (4 = Stock) |
| `isActive` | `Api.DefaultIsActive` (true) |

```json
{
  "hospitalId": "83340a8d-ca2c-4fd0-a6dc-367e28505752",
  "rfidCardNumber": "tesh",
  "rfidCardTypeId": "4f36704f-c6ff-4d4a-a23b-48778bba7718",
  "rfidCardBatchCode": "BATCH-001",
  "status": 4,
  "isActive": true
}
```

---

## Catalog (MGL reference)

| Kind | Id | Name |
|------|-----|------|
| Hospital | `83340a8d-ca2c-4fd0-a6dc-367e28505752` | Bệnh viện mắt Gia Lai |
| Card type | `0037d31e-9aca-4373-b744-e53a5e45457b` | Thẻ trắng |
| Card type | `4f36704f-c6ff-4d4a-a23b-48778bba7718` | Thẻ định danh |

IDs also live in `appsettings.json` under `Hospitals` / `CardTypes`.

---

## Response mapping

| Outcome | `RegistrationResult` |
|---------|----------------------|
| 2xx | `Ok(body)` |
| non-2xx | `Fail(RegistrationFailed, "{status} {body}")` |
| Exception / missing config | `Fail(RegistrationFailed, …)` |

Adapter validates BaseUrl, in-memory JWT session, hospitalId GUID, card type GUID, batch, and card number before calling HTTP.

---

## Call path

```text
MainForm (hospital + type + batch)
  → CardWriteOrchestrator
    → CardRegistrationService (BR-004: IsVerified)
      → HttpCardRegistrarAdapter
        → POST {BaseUrl}/api/rfid/cards
```

Workflow remains **Write → Verify → Register**. Do not register without verify.

---

## Legacy CardWritter (obsolete)

| Item | Legacy | Current (correct) |
|------|--------|-------------------|
| Route | `/odata/rfid/RfidTags` | `/api/rfid/cards` |
| Body | `EPCCode`, `RfidTagTypeId`, `RfidTagBatchCode` | camelCase Upsert fields + `hospitalId` |
| JSON | PascalCase | camelCase |

Do **not** restore OData unless Backend reintroduces it.

---

## Security & ops notes

| Topic | Note |
|-------|------|
| Bearer token | Prefer `appsettings.Local.json`; rotate when expired; avoid committing secrets |
| HTTPS | Production BaseUrl uses HTTPS |
| Hospital scope | JWT may carry `hospitalId`; body also sends `hospitalId` |
| Health UI | Config readiness only — does not HTTP-ping the API |

**Debt (accepted):** static token until login flow; sync-over-async HTTP (low).

---

## Compatibility checklist

| Check | Status |
|-------|--------|
| Client ↔ `POST /api/rfid/cards` | Pass (adapter + smoke) |
| Body matches CareHR Upsert request | Pass |
| Hospital + card types from catalog / appsettings | Pass |
| Write → Verify → Register workflow | Pass |

Keep `/api/rfid/cards` as the only create-card path for UhfCardWriter.
