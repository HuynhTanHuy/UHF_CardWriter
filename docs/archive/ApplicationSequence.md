# Application Sequence — Phase 6C

**Related:** [ApplicationWorkflow.md](ApplicationWorkflow.md), Services under `Application/Services`

---

## Write Card (happy path)

```mermaid
sequenceDiagram
    actor Op as Operator
    participant Orch as CardWriteOrchestrator
    participant Conn as CardConnectionService
    participant Scan as CardScanningService
    participant Write as CardWritingService
    participant Verify as CardVerificationService
    participant Reg as CardRegistrationService
    participant Ports as ICard* ports

    Note over Op,Conn: Reader already Connected (UC-001)
    Op->>Orch: RunWriteCardJob(request)
    Orch->>Conn: IsConnected
    Conn-->>Orch: true

    Orch->>Scan: ScanForSingleCard
    Scan->>Ports: StartScan / TryGetCard / StopScan
    Scan-->>Orch: ScanResult (Single)

    Orch->>Scan: SelectCard
    Scan->>Ports: SelectByIdentity
    Scan-->>Orch: OK

    Orch->>Write: WriteIdentity
    Write->>Ports: WriteEpc
    Write-->>Orch: CardWriteResult

    Orch->>Verify: Verify
    Verify->>Ports: ReadEpc (via CardReadingService)
    Verify-->>Orch: CardVerifyResult (Match)

    Orch->>Reg: Register(IsVerified=true)
    Reg->>Ports: ICardRegistrar.Register
    Reg-->>Orch: RegistrationResult OK
    Orch-->>Op: CardWriteJobResult Completed
```

---

## Verify only (UC-008)

```mermaid
sequenceDiagram
    participant Caller as Orchestrator / UI
    participant Verify as CardVerificationService
    participant Read as CardReadingService
    participant Port as ICardReader

    Caller->>Verify: Verify(intended, password)
    Verify->>Read: ReadCardIdentity(password, wordCount)
    Read->>Port: ReadEpc
    Port-->>Read: CardReadResult
    Read-->>Verify: CardIdentity
    Verify->>Verify: Compare EPC (BR-003)
    Verify-->>Caller: CardVerifyResult Match|Mismatch|Fail
```

---

## Register (UC-009)

```mermaid
sequenceDiagram
    participant Caller as CardWriteOrchestrator
    participant Reg as CardRegistrationService
    participant Port as ICardRegistrar

    Caller->>Reg: Register(request, IsVerified=true)
    alt IsVerified = false
        Reg-->>Caller: BusinessException (BR-004)
    else verified
        Reg->>Port: Register(request)
        Port-->>Reg: RegistrationResult
        Reg-->>Caller: RegistrationResult
    end
```

---

## Register fail after verify

```mermaid
sequenceDiagram
    participant Orch as CardWriteOrchestrator
    participant Reg as CardRegistrationService

    Note over Orch: Write OK + Verify Match already done
    Orch->>Reg: Register(...)
    Reg-->>Orch: Success=false
    Orch-->>Orch: Stage = WrittenButUnregistered (BR-009)
    Note over Orch: No rewrite; Operator may retry register only
```
