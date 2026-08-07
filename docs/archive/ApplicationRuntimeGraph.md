# Application Runtime Graph — Phase 6D

**Date:** 2026-08-06  
**Composition:** `AddCareHrCardWriter` / `App.CompositionRoot.CreateServiceProvider`

---

## End-to-end dependency graph

```text
UI (Phase 7) / CompositionRoot
        │
        ▼
┌───────────────────────────────────────────────────────────┐
│ Application Services (Singleton)                          │
│  CardConnectionService                                    │
│  CardScanningService                                      │
│  CardReadingService                                       │
│  CardWritingService                                       │
│  CardVerificationService                                  │
│  CardRegistrationService                                  │
│  CardWriteOrchestrator                                    │
└───────────────────────────┬───────────────────────────────┘
                            │ depends on ports only
        ┌───────────────────┼───────────────────┐
        ▼                   ▼                   ▼
 ICardConnection      ICardScanner         ICardWriter
 ICardReader          ICardSecurity        ICardRegistrar
        │                   │                   │
        ▼                   ▼                   ▼
 CardConnectionAdapter  CardScannerAdapter  CardWriterAdapter
 CardReaderAdapter      CardSecurityAdapter HttpCardRegistrarAdapter
        │                   │                   │
        └─────────┬─────────┘                   │
                  ▼                             ▼
            IUhfSdk (UhfPrimeSdk)         CareHR HTTP API
                  │                    (OData RfidTags)
                  ▼
            UhfPrimeDriver
                  │
                  ▼
            Native (UHFPrimeReader.dll)
```

---

## Port → Adapter → Downstream

| Port | Adapter | Downstream |
|------|---------|------------|
| `ICardConnection` | `CardConnectionAdapter` | `IUhfSdk.Connection` |
| `ICardScanner` | `CardScannerAdapter` | `Inventory` + `TagControl.Select` |
| `ICardWriter` | `CardWriterAdapter` | `Writer.Write` (EPC) |
| `ICardReader` | `CardReaderAdapter` | `Reader.Read` (EPC) |
| `ICardSecurity` | `CardSecurityAdapter` | `TagControl.Lock/Kill` |
| `ICardRegistrar` | `HttpCardRegistrarAdapter` | CareHR `POST /api/rfid/cards` |

---

## DI registration entry points

| Method | Project | Registers |
|--------|---------|-------------|
| `AddApplicationServices` | Application | Application Services + Orchestrator |
| `AddUhfInfrastructure` | Infrastructure | `IUhfSdk`, all `ICard*` adapters, `CareHrCardApiOptions` |
| `AddCareHrCardWriter` | Infrastructure | Both (full runtime) |
| `CompositionRoot.CreateServiceProvider` | App | Host composition (no UI) |

---

## Exception translation path

```text
Native / Driver exceptions
  → SdkException (SDK)
    → DeviceException (Infrastructure DeviceExceptionTranslator)
      → DeviceResult / OperationException (Application Services)
```

HTTP registry failures return `RegistrationResult.Fail` — no Sdk/Native types.
