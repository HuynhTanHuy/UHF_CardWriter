# Infrastructure Dependency Map

**Phase:** 6D (runtime integration)  
**Related:** [ApplicationRuntimeGraph.md](ApplicationRuntimeGraph.md)

---

## End-to-end graph

```text
Application (Services → ICard* ports)
        ↓
Infrastructure (Card*Adapter / HttpCardRegistrarAdapter)
        ↓
SDK Wrapper (IUhfSdk)  |  CareHR HTTP API
        ↓
Driver → Native → UHFPrimeReader.dll
```

---

## Port → Adapter → Downstream

| Application port | Adapter | Downstream |
|------------------|---------|------------|
| `ICardConnection` | `CardConnectionAdapter` | `IUhfSdk.Connection` |
| `ICardScanner` | `CardScannerAdapter` | `Inventory` + `TagControl.Select` |
| `ICardWriter` | `CardWriterAdapter` | `Writer.Write` (EPC bank, wordPtr=2) |
| `ICardReader` | `CardReaderAdapter` | `Reader.Read` (EPC bank, wordPtr=2) |
| `ICardSecurity` | `CardSecurityAdapter` | `TagControl.Lock` / `Kill` |
| `ICardRegistrar` | `HttpCardRegistrarAdapter` | `POST {BaseUrl}/odata/rfid/RfidTags` |

---

## Result map

| Downstream | Application |
|------------|-------------|
| `SdkResult` + STAT_* | `DeviceResult` + `DeviceErrorCode` |
| SDK `TagIdentity` | `CardInformation` / `CardIdentity` |
| SDK `TagAccessResponse` | `CardWriteResult` |
| SDK `TagReadData` | `CardReadResult` |
| `SdkException` | `DeviceException` (then Services map) |
| HTTP non-success / network | `RegistrationResult.Fail` |
