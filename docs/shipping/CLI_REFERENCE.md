# Crown & Conquest — Command-Line Interface (CLI) Reference

## 1. Overview
Crown & Conquest supports headless execution, automated smoke testing, deterministic simulation replay, benchmark suites, and environment validation via command-line arguments.

---

## 2. Command Switches Reference

| Switch | Argument / Type | Description | Default |
|:---|:---|:---|:---|
| `--headless` | None (Flag) | Launches the simulation engine in headless non-graphical mode for CI/CD and automation | `false` |
| `--smoke-test` | None (Flag) | Executes the automated 600-tick end-to-end smoke test harness and outputs process exit codes (0 = Success) | `false` |
| `--benchmark` | `[ticks]` (Integer) | Runs high-scale simulation performance benchmark (500+ units) and validates frame budget | `1000` |
| `--validate-env` | None (Flag) | Runs clean-machine hardware, OS, memory, and runtime diagnostics | `false` |
| `--scenario` | `[name]` (String) | Launches a specific simulation scenario (`CombatArena`, `SettlementEconomy`, `SiegeWarfare`, `ReleaseCandidate`, `BalanceAndValidation`) | `Main` |
| `--seed` | `[number]` (Integer) | Sets the initial pseudo-random number generator seed for 100% deterministic replay | `42` |
| `--save-path` | `[path]` (String) | Specifies target file path for saving/loading simulation states | `savegame.json` |
| `--ticks` | `[count]` (Integer) | Specifies number of fixed simulation ticks to execute before terminating | `1000` |
| `--log-level` | `Debug` / `Info` / `Warn` / `Error` | Configures domain logger output verbosity | `Info` |

---

## 3. Standard Process Exit Codes

| Exit Code | Classification | Description |
|:---:|:---|:---|
| `0` | **Success** | Simulation, smoke test, or benchmark completed cleanly with all invariants preserved |
| `1` | **Simulation Invariant Failure** | Game rule or combat math invariant violated during simulation playout |
| `2` | **State Corruption** | Save/load deserialization mismatch or unhandled data schema corruption |
| `3` | **Execution Error / Timeout** | Fatal environment defect, missing asset file, or execution timeout |
| `4` | **Hardware Incompatible** | Environment diagnostics rejected system (e.g. 32-bit architecture or insufficient RAM) |

---

## 4. Usage Examples

```powershell
# Run automated headless smoke test in CI/CD pipeline
CrownConquest.exe --headless --smoke-test --seed 42

# Execute 1,000-tick performance certification benchmark
CrownConquest.exe --headless --benchmark 1000 --seed 1337

# Validate clean-machine environment prerequisites
CrownConquest.exe --validate-env
```
