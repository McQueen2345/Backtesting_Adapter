# QTS.Edge mit Backtest System

## Projekt-Struktur

```
QTS.Edge.sln
│
├── 📁 Edge (Production)
│   ├── QTS.Edge.Core              → StructImbL1Edge Algorithmus
│   └── QTS.Edge.TradeManagement   → Position Tracking, Risk Management
│
├── 📁 Backtest (Neu)
│   ├── QTS.Backtest.Contracts     → Shared Interfaces & DTOs
│   ├── QTS.Backtest.Adapter       → TTD → DOM Snapshots
│   ├── QTS.Backtest.Engine        → Simulation Engine
│   └── QTS.Backtest.Harness       → Verbindet Edge + Engine
│
└── 📁 Tests
    ├── QTS.Edge.Tests
    ├── QTS.Edge.TradeManagement.Tests
    ├── QTS.Backtest.Adapter.Tests
    ├── QTS.Backtest.Engine.Tests
    └── QTS.Backtest.Integration.Tests
```

## Quick Start

```bash
# Solution öffnen
cd QTS.Edge
code .

# Restore & Build
dotnet restore
dotnet build

# Tests ausführen
dotnet test
```

## VSCode Tasks

| Task | Beschreibung |
|------|--------------|
| `build` | Baut die gesamte Solution |
| `test` | Führt alle Tests aus |
| `test-adapter` | Nur Adapter-Tests |
| `test-engine` | Nur Engine-Tests |
| `test-integration` | Nur E2E-Tests |
| `watch-adapter-tests` | Watch-Mode für Adapter |
| `watch-engine-tests` | Watch-Mode für Engine |

## Implementierungs-Reihenfolge

### Phase 1: Adapter (41 Sub-Tasks)
Siehe: `QTS_Backtest_Adapter_TaskSlicing_v1_0.docx`

**Kritisch:**
- T7: TimestampParser (UTC, ns/µs/ms)
- T9: DomState (Bids Descending!)

### Phase 2: Engine (47 Sub-Tasks)
Siehe: `QTS_Backtest_Engine_TaskSlicing_v1_0.docx`

**Kritisch:**
- T6: SessionManager (ETH Wrap-Around)
- T7: OrderSimulator (FillSeq, KEIN Guid!)
- T11: Event-Loop (7 Phasen)

## Spezifikationen (EINGEFROREN)

| Dokument | Version | Status |
|----------|---------|--------|
| Funktionsbeschreibung | v0.2 | 🔒 FROZEN |
| Adapter-Spec | v1.0.1 | 🔒 FROZEN |
| Engine-Spec | v1.1.2 | 🔒 FROZEN |

## RED LIST - Nicht verhandelbar

- 🔴 KEIN `Guid.NewGuid()` - Verwendet `long FillSeq`
- 🔴 KEIN Auto-Flip - Flip erfordert 2 Orders in 2 Snapshots
- 🔴 Event-Loop Reihenfolge ist NICHT VERHANDELBAR
- 🔴 Alle Timestamps MÜSSEN UTC sein (`DateTimeKind.Utc`)
- 🔴 CSV-Export MUSS byte-identisch sein

## Backtest-Flow

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│  TTD Files      │───▶│  Adapter        │───▶│  IDomSnapshot   │
│  (ZIP/CSV)      │    │  TtdSnapshot    │    │  Stream         │
└─────────────────┘    │  Source         │    └────────┬────────┘
                       └─────────────────┘             │
                                                       ▼
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│  BacktestResult │◀───│  Engine         │◀───│  Harness        │
│  (Trades, PnL)  │    │  BacktestRunner │    │  StructImb      │
└─────────────────┘    └─────────────────┘    │  BacktestHarness│
                                              └─────────────────┘
                                                       │
                                              ┌────────┴────────┐
                                              │                 │
                                              ▼                 ▼
                                    ┌─────────────────┐ ┌─────────────────┐
                                    │ StructImbL1Edge │ │ TradeManager    │
                                    │ (Dein Algo!)    │ │ (Risk, Position)│
                                    └─────────────────┘ └─────────────────┘
```

## Nächste Schritte

1. **Öffne das Projekt in VSCode**
2. **Starte mit Adapter Phase 1, Task T1.1**
3. **Folge dem Task-Slicing Dokument**

---

*QTS.Edge Backtest System - Version 1.0*
*Basierend auf Adapter-Spec v1.0.1 und Engine-Spec v1.1.2*
