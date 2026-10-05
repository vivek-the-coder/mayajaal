# MayaJaal

**Local-first Windows cyber defense** — honey (decoy) files, encrypted vault lockdown, and a faculty-friendly Attack Lab.

MayaJaal watches for USB, process, file, and decoy activity. When signals correlate into an attack pattern, it can **reversibly lock** your encrypted vault so sensitive files stay contained.

> Not a full antivirus replacement. It focuses on **file-theft / insider / USB exfil behavior** with explainable scoring and a demo-ready UI.

---

## Why use it?

| Need | What MayaJaal does |
|------|--------------------|
| Protect important files | Encrypt them into a vault; lockdown on threat |
| Catch sneaky access | Honey/decoy files trip high-confidence alerts |
| Teach / demo defense | Attack Lab runs USB & insider scenarios in one window |
| Stay local | No cloud SIEM required — Guardian + Security Center on the PC |

---

## How protection works

```
Sensors (USB · process · honey · mass-copy)
        ↓
ThreatEngine (risk + confidence)
        ↓
CorrelationEngine (attack patterns)
        ↓
PolicyEngine + SafetyGate
        ↓
Incident + Vault lockdown (reversible)
```

1. **On Guardian start** — decoy files are deployed (bait is ready *before* an attack).
2. **You add real files** — Unlock vault → Add file → contents are AES-256-GCM encrypted.
3. **During an attack** — if someone touches a decoy + USB/process/mass-copy, MayaJaal scores CRITICAL and can lock the vault.
4. **After the demo** — Resolve / Rollback from Incidents.

---

## Features

- Encrypted vault (Unlock / Add file / Lock)
- Honey decoy files (credentials, keys, finance-style bait)
- USB, process, and mass-copy monitoring
- Risk & confidence scoring with named correlation patterns
- Policy responses: MONITOR → ALERT → LOCK_VAULT → EMERGENCY_LOCKDOWN
- Incidents, evidence, and rollback
- **Attack Lab** — one-click `usb-exfil` and `insider` demos
- Security Center WPF console (Dashboard, Incidents, Vault, Policies, Detections, Settings)

---

## Tech stack

| Layer | Stack |
|-------|--------|
| Runtime | C# / .NET 8 |
| Guardian | Hosted worker / Windows Service, Serilog |
| Domain | Risk, Confidence, Correlation, Policy, SafetyGate |
| Storage | SQLite event store |
| Crypto | AES-256-GCM, Argon2id |
| IPC | ACL-hardened named pipes (JSON framed) |
| UI | WPF + CommunityToolkit.Mvvm |
| Tests | xUnit |

---

## Quick start

### Prerequisites

- Windows 10/11  
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Setup

```powershell
.\scripts\setup.ps1
```

Or:

```powershell
dotnet restore MayaJaal.sln
dotnet build MayaJaal.sln -c Release
dotnet test tests\MayaJaal.Tests\MayaJaal.Tests.csproj -c Release
```

### Live demo (recommended)

```powershell
.\scripts\run-demo.ps1
```

Opens **Security Center**; Guardian starts hidden in the background.

**Faculty path:** Attack Lab → choose scenario → **Run attack simulation** → show Dashboard / Incidents / Vault.

### Run separately

```powershell
# Terminal 1 — Guardian
dotnet run --project src\MayaJaal.Guardian -- --console

# Terminal 2 — Security Center
dotnet run --project src\MayaJaal.SecurityCenter
```

| Flag | Behavior |
|------|----------|
| `--console` | Run Guardian as a console host |
| `--simulate` | Continuous demo telemetry |
| `--demo` | One-shot scripted scenario, then exit |

---

## Attack Lab examples

### USB exfiltration (`usb-exfil`)

1. USB insert  
2. Suspicious process (`exfil-tool.exe`)  
3. Honey file access  
4. Mass copy to USB  

→ Pattern `USB_Process_MassCopy` → high risk → vault lockdown.

### Insider harvest (`insider`)

1. Open decoy credentials  
2. Copy / stage files  
3. Mass file activity  

→ Pattern `Insider_Credential_Exfil` → incident + containment.

---

## Solution layout

| Project | Role |
|---------|------|
| `MayaJaal.Shared` | Models, contracts, IPC client |
| `MayaJaal.Domain` | Scoring & policy engines |
| `MayaJaal.Infrastructure` | SQLite, vault, crypto, honey files |
| `MayaJaal.Application` | DI composition |
| `MayaJaal.Guardian` | Threat engine + named-pipe server |
| `MayaJaal.SecurityCenter` | WPF security console |
| `MayaJaal.Tests` | Unit tests |

---

## Docs

- [Architecture overview](docs/architecture.md)
- [Comprehensive engineering report](docs/MAYAJAAL-COMPREHENSIVE-REPORT.md)

---

## Scripts

| Script | Purpose |
|--------|---------|
| `scripts/setup.ps1` | Restore / build / test |
| `scripts/run-demo.ps1` | Single-window faculty demo |
| `scripts/full-test.ps1` | IPC + UI + unit full verification |
| `scripts/install-service.ps1` | Install Guardian as a Windows service |

---

## License / coursework

Capstone / academic project by [vivek-the-coder](https://github.com/vivek-the-coder).

Repository: https://github.com/vivek-the-coder/mayajaal
