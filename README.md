# MayaJaal

MayaJaal is a local cyber-defense platform: a Windows Guardian service scores risk from decoy and file-system signals, and the Security Center WPF console visualizes threat state over named-pipe IPC.

## Prerequisites

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Setup

```powershell
.\scripts\setup.ps1
```

Or manually:

```powershell
dotnet restore MayaJaal.sln
dotnet build MayaJaal.sln -c Release
dotnet test tests\MayaJaal.Tests\MayaJaal.Tests.csproj -c Release
```

## Run Guardian (console)

```powershell
dotnet run --project src\MayaJaal.Guardian -- --console
```

Optional flags:

| Flag | Behavior |
|------|----------|
| `--console` | Run as console host (not Windows Service) |
| `--simulate` | Inject continuous demo telemetry (keeps IPC up) |
| `--demo` | One-shot scripted scenario, then exit |

## Run Security Center

```powershell
dotnet run --project src\MayaJaal.SecurityCenter
```

Security Center polls `GuardianClient` every 2 seconds (`GetStatusAsync`, `GetEventsAsync`, `GetIncidentAsync`, `GetIncidentsAsync`, `GetVaultsAsync`, `GetPoliciesAsync`). Operators can lock vaults, resolve incidents, and execute rollback from the console. If Guardian is down, the UI shows an offline banner.

## Live demo (single window)

```powershell
.\scripts\run-demo.ps1
```

Builds the solution and launches **Security Center only**. Guardian starts hidden in the background automatically.

For faculty demos: open **Attack Lab** → choose a scenario → **Run attack simulation**, then show **Dashboard** / **Incidents** for detection and lockdown.

## Solution layout

| Project | Role |
|---------|------|
| `MayaJaal.Shared` | Models, contracts, IPC client |
| `MayaJaal.Domain` | Risk / confidence / correlation / policy engines |
| `MayaJaal.Infrastructure` | SQLite event store, vault, crypto, honey files |
| `MayaJaal.Application` | DI composition helpers |
| `MayaJaal.Guardian` | Hosted threat engine + named-pipe server |
| `MayaJaal.SecurityCenter` | WPF security console |
| `MayaJaal.Tests` | xUnit unit tests for domain engines |

## Documentation

- [Comprehensive Engineering Report](docs/MAYAJAAL-COMPREHENSIVE-REPORT.md) — professional full specification: engines, schemas, IPC, runbooks, ADRs, verification, deep annex (v3.0.0)
- [Architecture overview](docs/architecture.md) — concise layer diagram and IPC summary

