# Application Ports — Discovery

**Phase:** 6A  
**Rule:** Ports come from Services; never mirror SDK facet names  
**Related:** [ApplicationServices.md](ApplicationServices.md), [DomainModel.md](DomainModel.md)

---

## Ports

| Port | Consumed by | Capability |
|------|-------------|------------|
| **ICardConnection** | CardConnectionService | OpenSerial / OpenHid / OpenNet / Close / IsOpen / ListUsbReaders |
| **ICardScanner** | CardScanningService | StartScan / TryGetCard / StopScan / SelectByIdentity |
| **ICardWriter** | CardWritingService | WriteIdentity (intended EPC + password) — **no MemBank in signature** |
| **ICardReader** | CardReadingService, CardVerificationService | ReadIdentity |
| **ICardRegistrar** | CardRegistrationService | Register(identity, type, batch) |
| **ICardSecurity** *(optional)* | CardSecurityService | Lock / Kill |

---

## Why not SDK names

| Avoid | Prefer |
|-------|--------|
| `IUhfConnection` | `ICardConnection` |
| `IUhfInventory` | `ICardScanner` |
| `IUhfWriter` | `ICardWriter` |
| `IUhfReader` | `ICardReader` |
| `IUhfTagControl` | `ICardScanner.Select*` + optional `ICardSecurity` |

Infrastructure adapts these ports → `IUhfSdk` (SDK language stays below the boundary).

---

## Port ownership

```text
Application Service
    → Application Port (ICard*)
        → Infrastructure Adapter
            → SDK Wrapper (IUhfSdk)
```

---

## Out of Application ports

| Concern | Layer |
|---------|-------|
| HTTP details of CareHR OData | Infrastructure adapter behind `ICardRegistrar` |
| Gen2 bank / wordPtr defaults | Infrastructure adapter behind `ICardWriter` |
| DllImport | Native (never Application) |
