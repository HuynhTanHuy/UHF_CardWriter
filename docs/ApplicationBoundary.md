# Application Boundary — Language & Layers

**Phase:** 6A  
**Related:** [ApplicationUseCases.md](ApplicationUseCases.md), [ApplicationPorts.md](ApplicationPorts.md), [ApplicationBoundaryReview.md](ApplicationBoundaryReview.md)

---

## Layer languages (must not mix)

| Layer | Language | Examples |
|-------|----------|----------|
| **Application** | CareHR / Card domain | Card, CardIdentity, ICardWriter, Register, Verify |
| **Infrastructure** | Mapping + adapters | `CardWriterAdapter` → `IUhfSdk.Writer` |
| **SDK Wrapper** | Vendor-friendly managed API | `IUhfSdk`, `SdkResult`, `MemBank` |
| **Driver** | Interop | `UhfPrimeDriver`, `NativeResult` |
| **Native** | CFApi / DLL | `DllImport`, `STAT_*`, `NativeTagInfo` |

---

## Interaction pattern (all device Use Cases)

```text
Operator
  → Application (Use Case / Service)
    → Application Port (ICard*)
      → Infrastructure Adapter
        → SDK Wrapper (IUhfSdk)
          → Driver
            → Native
              → Reader
                → Card
```

Registration Use Case replaces the device branch with:

```text
Application → ICardRegistrar → Infrastructure HTTP adapter → CareHR API
```

---

## Per–Use Case interaction (summary)

| UC | Application | Port | Downstream |
|----|-------------|------|------------|
| UC-001/002/003 | CardConnectionService | ICardConnection | SDK Connection |
| UC-004/005/010 | CardScanningService | ICardScanner | SDK Inventory + Select |
| UC-006 | CardReadingService | ICardReader | SDK Reader |
| UC-007 | CardWritingService | ICardWriter | SDK Writer (+ select already done) |
| UC-008 | CardVerificationService | ICardReader | SDK Reader + compare in Application |
| UC-009 | CardRegistrationService | ICardRegistrar | CareHR HTTP |
| UC-011/012 | CardSecurityService | ICardSecurity | SDK TagControl |

---

## Boundary review

| Check | Status |
|-------|--------|
| Application must not reference Sdk | Required (already true for project refs) |
| Application must not expose MemBank/STAT/Uhf ports | Required for target model |
| Infrastructure sole Sdk + HTTP owner | Required |
| Driver/Native invisible above Sdk | Required |
| Current interim `IUhf*` in Application | **Debt** — migrate per plan |

---

## Architecture decision

1. **Application Language = Card / CareHR domain.**  
2. **SDK Language stays in Sdk project only.**  
3. **Infrastructure translates** domain ports ↔ Sdk (+ HTTP).  
4. **No permanent ADR keeping Application on Uhf terminology** (rejected in Phase 5A).
