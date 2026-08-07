# Architecture — CareHR UHF Card Writer

Short overview of layers, Clean Architecture, and where to read next.
Living document — phase reports archived under docs/archive/.

---

## Layers

```text
App (WinForms UI, CompositionRoot, config, diagnostics)
  → Application (Use cases, Services, ICard* ports, DTOs, orchestrator)
    → Infrastructure (Card*Adapter, HttpCardRegistrarAdapter)
      → Sdk (IUhfSdk / UhfPrimeSdk → UhfPrimeDriver → Native P/Invoke)
        → UHFPrimeReader.dll + hidapi.dll
```

Registration bypasses the device stack:

```text
Application → ICardRegistrar → HttpCardRegistrarAdapter → CareHR POST /api/rfid/cards
```

---

## Clean Architecture rules

| Rule | Practice |
|------|----------|
| Dependency direction | Inner layers do not reference outer UI/infra details |
| Domain language in Application | `Card*`, `ICard*`, Verify, Register — not MemBank / STAT / DllImport |
| SDK language below boundary | `IUhfSdk`, `SdkResult`, Gen2 banks stay in Sdk + Infrastructure mapping |
| Driver is thin | Single native calls; no inventory loops, retry, or business rules |
| One reader session | Singleton `IUhfSdk` + serialize access (not thread-safe) |

---

## Primary workflow

```text
Connect → Scan (one card) → Select → Write EPC → Verify → Register → Complete
```

On register failure after successful verify: **WrittenButUnregistered** — retry register only; do not auto-rewrite.

Details: [Application.md](Application.md).

---

## Composition entry points

| Method | Role |
|--------|------|
| `AddApplicationServices` | Application services + orchestrator |
| `AddUhfInfrastructure` | `IUhfSdk` + `ICard*` adapters + API options |
| `AddCareHrCardWriter` | Full Application + Infrastructure |
| `CompositionRoot.CreateServiceProvider` | App host wiring |

---

## Documentation map

| Doc | Content |
|-----|---------|
| [Application.md](Application.md) | Domain, BRs, UCs, services, ports, workflow, DI graph |
| [Driver.md](Driver.md) | `UhfPrimeDriver` contract, exceptions, marshal, buffers, threads |
| [Infrastructure.md](Infrastructure.md) | Adapters, lifetimes, port→SDK/HTTP map |
| [SDK.md](SDK.md) | Public `IUhfSdk` surface + vendor report pointer |
| [API.md](API.md) | Production CareHR create-card HTTP contract |
| [Configuration.md](Configuration.md) | appsettings keys and Local override |
| [Operations.md](Operations.md) | Ops overview + log paths |
| [ReleaseNotes.md](ReleaseNotes.md) | RC status and limitations |
| [RepositoryGuide.md](RepositoryGuide.md) | Repo structure and docs strategy |
| [SupportGuide.md](SupportGuide.md) | Full field troubleshooting |

ADRs under [docs/adr/](adr/). Phase gate reports belong in [docs/archive/](archive/).
