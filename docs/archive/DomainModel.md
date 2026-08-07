# Domain Model — CareHR Card Writer

**Phase:** 6A  
**Rule:** Business language only — no UHF SDK type names as domain nouns  
**Related:** [ApplicationUseCases.md](ApplicationUseCases.md)

---

## Core domain objects

| Object | Meaning |
|--------|---------|
| **Card** | Physical CareHR RFID card being issued/updated |
| **CardIdentity** | Business identity of the card as EPC string/bytes known to CareHR |
| **CardContent** | Readable payload of interest (primarily EPC; future user memory if needed) |
| **IntendedCardIdentity** | EPC the Operator/job wants written |
| **CurrentCardIdentity** | EPC currently on the physical card (from scan/read) |
| **CardWriteJob** | One Operator attempt: inputs + outcomes (scan → write → verify → register) |
| **CardRegistration** | Backend persistence of a verified card identity |
| **CardBatch** | Issuance batch metadata (`RfidTagBatchCode`) |
| **CardType** | CareHR RFID tag type (`RfidTagTypeId`) |
| **Reader** | Physical desk reader device |
| **ReaderConnection** | Active session to a Reader |
| **ReaderEndpoint** | How to reach reader (COM / USB index / IP:port) |
| **ReaderStatus** | Connected / Disconnected / Busy / Error |
| **OperationOutcome** | Success/failure of a business step with `DeviceErrorCode`-level meaning |
| **OperatorSession** | UI session context (selected endpoint, last job) |

---

## Relationships (conceptual)

```text
OperatorSession
  └─ ReaderConnection ──► Reader (via ReaderEndpoint)
  └─ CardWriteJob
        ├─ CurrentCardIdentity (from scan)
        ├─ IntendedCardIdentity (from CareHR business input)
        ├─ CardBatch / CardType
        ├─ verify result
        └─ CardRegistration (after API)
```

---

## Explicit non-domain (keep out of Domain language)

| Term | Belongs to |
|------|------------|
| MemBank, wordPtr, InventoryContinue | SDK / Infrastructure |
| NativeTagInfo, STAT_* | Native / Driver |
| TagInfo, TagResp | Vendor SDK samples |
| DEA_*, MIFARE block | Excluded product path |

---

## Invariants (business)

1. **No register without verify success.**  
2. **Prefer single card in field** for Scan/Write.  
3. **Physical write before CareHR register** (same order as CardWritter intent).  
4. **API failure after verify** ≠ automatic rewrite; Operator reconciles.
