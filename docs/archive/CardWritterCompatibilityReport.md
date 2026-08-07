# CardWritter Compatibility Report

**Phase:** 8 — Backend Integration (corrected to live CareHR API)  
**Date:** 2026-08-07  
**Authoritative contract:** CareHR frontend / production API (operator-provided curl), not legacy CardWritter OData.

---

## Endpoint Mapping

| Item | Value |
|------|--------|
| Method | `POST` |
| Route | `{BaseUrl}/api/rfid/cards` |
| BaseUrl (MGL) | `https://carehr02-mgl-api.2bsolu.com` |
| Auth | `Authorization: Bearer {token}` |
| Accept | `application/json, text/plain, */*` |
| Content-Type | `application/json` |

---

## DTO Mapping (request body)

| Wire field | Source |
|------------|--------|
| `hospitalId` | Selected hospital / `RegistrationRequest.HospitalId` |
| `rfidCardNumber` | ASCII decode of EPC when printable; else EPC hex |
| `rfidCardTypeId` | Selected card type |
| `rfidCardBatchCode` | Batch text box / default `BATCH-001` |
| `status` | `Api.DefaultStatus` (4 = Stock) |
| `isActive` | `Api.DefaultIsActive` (true) |

Example:

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

## Catalog (MGL)

| Kind | Id | Name |
|------|-----|------|
| Hospital | `83340a8d-ca2c-4fd0-a6dc-367e28505752` | Bệnh viện mắt Gia Lai |
| Card type | `0037d31e-9aca-4373-b744-e53a5e45457b` | Thẻ trắng |
| Card type | `4f36704f-c6ff-4d4a-a23b-48778bba7718` | Thẻ định danh |

---

## Response Mapping

| Outcome | `RegistrationResult` |
|---------|----------------------|
| 2xx | `Ok(body)` |
| non-2xx | `Fail(RegistrationFailed, "{status} {body}")` |
| Exception | `Fail(RegistrationFailed, ex.Message)` |

---

## Known Differences vs legacy CardWritter

| Item | Legacy CardWritter | Current (correct) |
|------|--------------------|-------------------|
| Route | `/odata/rfid/RfidTags` | `/api/rfid/cards` |
| Body | `EPCCode`, `RfidTagTypeId`, `RfidTagBatchCode` | camelCase UpsertRFIDCardRequest fields + `hospitalId` |
| JSON casing | PascalCase | camelCase |

Legacy OData path is **obsolete** for CareHR MGL; do not restore unless Backend reintroduces it.

---

## Compatibility Result

| Check | Result |
|-------|--------|
| Client ↔ CareHR `POST /api/rfid/cards` | **PASS** (adapter + smoke) |
| Workflow Write → Verify → Register | **PASS** (unchanged) |
