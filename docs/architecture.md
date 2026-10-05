# MayaJaal Architecture

## Overview

MayaJaal separates **signal collection and response** (Guardian) from **operator visualization** (Security Center). Shared models and a named-pipe IPC client live in `MayaJaal.Shared`.

```
[File / USB / Honey signals]
            |
            v
   EventCollector  --->  ThreatEngine  --->  IncidentService / EventStore
            |                  |
            |                  +-- RiskEngine
            |                  +-- ConfidenceEngine
            |                  +-- CorrelationEngine
            |                  +-- PolicyEngine / SafetyGate
            v
        IpcServer  <---- named pipe "MayaJaal.Guardian" ---->  Security Center
                                                                  (GuardianClient)
```

## Layers

- **Shared** — `SecurityEvent`, `Incident`, `ThreatState`, `ThreatLevel`, `ResponseAction`, contracts (`IThreatEngine`, …), `GuardianClient` / `IpcRequest` / `IpcResponse`.
- **Domain** — pure scoring and policy logic (no I/O).
- **Infrastructure** — SQLite event store, Argon2/AES vault crypto, honey-file and asset services.
- **Guardian** — host process: collectors, `ThreatEngine`, IPC server; supports `--console`, `--simulate`, `--demo`.
- **Security Center** — WPF MVVM console; `GuardianStatusService` polls status/events/incidents/vaults/policies every 2s and supports lock, resolve, and rollback commands.

## IPC framing

Length-prefixed UTF-8 JSON: `[int32 LE length][payload]`. Commands: `GET_STATUS`, `GET_INCIDENT`, `GET_EVENTS`, `GET_ASSETS`, `GET_VAULTS`, `GET_INCIDENTS`, `GET_POLICIES`, `LOCK_VAULT`, `RESOLVE_INCIDENT`, `EXECUTE_ROLLBACK`, `PING`.

## Threat pipeline

1. Collector emits `SecurityEvent`.
2. Risk + confidence + correlation evaluate the recent window.
3. Policy selects a `ResponseAction`; SafetyGate may block unsafe actions.
4. `IncidentService` opens/updates incidents and may lock vaults.
5. `ThreatState` is exposed to Security Center via IPC.
