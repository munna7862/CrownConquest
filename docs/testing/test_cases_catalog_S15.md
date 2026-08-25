# Sprint 15 Test Cases Catalog: Release Candidate and Shipping

## Document Control
- **Sprint:** Sprint 15 — Release Candidate and Shipping
- **Author:** QA & SDET Architect
- **Target Version:** v1.0.0-rc1
- **Status:** APPROVED (Pre-Implementation Baseline)

---

## Test Tier Architecture

| Tier | Focus Area | Framework | Target Execution Time |
|:---|:---|:---|:---|
| **Tier 1** | Pure Domain Unit Tests (Manifests, Checksums, Diagnostics, Save Resilience) | xUnit | $< 500\text{ ms}$ |
| **Tier 2** | Simulation Invariants & Fuzzing (Smoke Runner, Checksum Parity, Corrupt Save Fuzzing) | xUnit | $< 2.5\text{ s}$ |
| **Tier 3** | Multi-System Integration (Full Match Regression, Environment Validation, Performance Certifier) | xUnit | $< 5.0\text{ s}$ |
| **Tier 4** | Headless Scenario & Shipping Presentation (Release Candidate Scenario, CLI Smoke Suite) | xUnit / Headless | $< 10.0\text{ s}$ |

---

## Test Cases Matrix

### 1. Release Manifest & Checksum Validation (`CNC-1501`, `CNC-1503`)

| Test ID | Tier | Type | Description | Expected Outcome |
|:---|:---|:---|:---|:---|
| `TC-S15-001` | Tier 1 | Positive | Compute SHA-256 checksum for byte payload and string | Bit-for-bit exact 64-character lowercase hex digest matching standard SHA-256 |
| `TC-S15-002` | Tier 1 | Positive | Generate `ReleaseManifest` with valid files and verify integrity | `Sha256ChecksumValidator.VerifyManifest()` returns `Valid = true` with 0 mismatches |
| `TC-S15-003` | Tier 1 | Negative | Detect modified file content in release manifest | Returns `Valid = false` with mismatch error identifying corrupted file path |
| `TC-S15-004` | Tier 1 | Negative | Detect missing file referenced in release manifest | Returns `Valid = false` with missing file error |
| `TC-S15-005` | Tier 1 | Boundary | Empty manifest and zero-byte file verification | Handles empty and zero-byte files gracefully with correct SHA-256 hash (`e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855`) |
| `TC-S15-006` | Tier 1 | Positive | `PackageBundleGenerator` creates in-memory zip archive / file bundle | Package bundle contains all entries and manifest matches bundled contents |

### 2. Clean-Machine Environment Diagnostics (`CNC-1502`, `CNC-1510`)

| Test ID | Tier | Type | Description | Expected Outcome |
|:---|:---|:---|:---|:---|
| `TC-S15-007` | Tier 1 | Positive | `CleanMachineEnvironmentValidator.ValidateEnvironment()` on valid system | Returns `OverallStatus = Pass` with diagnostic details for OS, .NET, RAM, and Disk |
| `TC-S15-008` | Tier 1 | Negative | Diagnostic evaluation with insufficient memory (<2GB) | Returns warning/fatal diagnostic with remediation recommendation |
| `TC-S15-009` | Tier 1 | Negative | Diagnostic evaluation with non-x64 architecture | Returns fatal diagnostic blocking unsupported 32-bit/ARM runtime |
| `TC-S15-010` | Tier 1 | Boundary | Headless display fallback verification | Correctly identifies headless display adapter mode without GUI dependencies |

### 3. Headless Smoke Test Automation (`CNC-1504`)

| Test ID | Tier | Type | Description | Expected Outcome |
|:---|:---|:---|:---|:---|
| `TC-S15-011` | Tier 2 | Positive | `HeadlessSmokeTestRunner.RunSmokeTest()` 600 ticks | Executes full lifecycle (Init -> Spawn -> Economy -> Combat -> Save -> Reload -> Victory) returning `ExitCode = 0` |
| `TC-S15-012` | Tier 2 | Invariant | Smoke test state checksum comparison before save and after reload | Post-reload state matches pre-save state with identical entity attributes |
| `TC-S15-013` | Tier 2 | Negative | Smoke test with corrupted initial configuration | Returns non-zero exit code (`ExitCode = 1` or `2`) with descriptive failure reason |
| `TC-S15-014` | Tier 2 | Invariant | Smoke test execution completes within time budget | 600-tick headless smoke test finishes in $< 1.5\text{ s}$ |

### 4. Final Performance & Frame Budget Certification (`CNC-1505`)

| Test ID | Tier | Type | Description | Expected Outcome |
|:---|:---|:---|:---|:---|
| `TC-S15-015` | Tier 3 | Positive | `ReleasePerformanceCertifier.CertifyPerformance()` 1,000 ticks with 500+ units | Mean tick duration $< 16.6\text{ ms}$ (60fps compliant), peak tick $< 33.3\text{ ms}$ |
| `TC-S15-016` | Tier 3 | Invariant | Memory footprint during 1,000-tick certification run | Total allocated memory strictly $< 500\text{ MB}$ ($< 150\text{ MB}$ domain footprint) |
| `TC-S15-017` | Tier 3 | Invariant | Zero GC allocations per tick during steady-state combat loop | Steady-state combat and movement loop generates 0 dynamic heap allocations |

### 5. Save/Load Schema Resilience & Migration (`CNC-1506`)

| Test ID | Tier | Type | Description | Expected Outcome |
|:---|:---|:---|:---|:---|
| `TC-S15-018` | Tier 1 | Positive | `ReleaseSaveCompatibilityValidator` validates standard v1.0.0 save | Successfully parses and validates version header, entity payloads, and resources |
| `TC-S15-019` | Tier 1 | Negative | Truncated/corrupted JSON save payload | Returns `Result.Failure` with `GameError` without unhandled exception or crash |
| `TC-S15-020` | Tier 1 | Negative | Save payload with unsupported future schema version | Returns `Result.Failure` with version mismatch error and migration guidance |
| `TC-S15-021` | Tier 1 | Boundary | Save payload with missing optional attributes | Defaults missing attributes safely without corrupting state |

### 6. Full Match Multi-System Regression (`CNC-1508`)

| Test ID | Tier | Type | Description | Expected Outcome |
|:---|:---|:---|:---|:---|
| `TC-S15-022` | Tier 3 | Positive | `FullMatchRegressionHarness.RunFullMatchRegression()` | Executes full 1,500-tick multi-system regression (combat, tech, heroes, AI, formations, siege) cleanly |
| `TC-S15-023` | Tier 3 | Invariant | 1,000-tick deterministic replay parity across dual seeded runs | Replay checksum $Checksum_A == Checksum_B$ at tick 1,000 |

### 7. Release Candidate Presentation & CLI Verification (`CNC-1507`, `CNC-1509`)

| Test ID | Tier | Type | Description | Expected Outcome |
|:---|:---|:---|:---|:---|
| `TC-S15-024` | Tier 4 | Positive | `ReleaseCandidatePresenter` formats all readiness telemetry | Returns formatted view models with pass/fail badges, performance stats, and manifest details |
| `TC-S15-025` | Tier 4 | Positive | `ReleaseCandidateScenario` executes headless validation pass | Successfully executes release verification scenario and outputs release certification report |

---

## Pass / Fail Quality Gate Criteria
- **Pass:** 100% of test cases pass (25/25 new tests, 337+ cumulative tests). Zero failures, zero skipped tests.
- **Fail:** Any failed test, non-deterministic checksum divergence, hot-loop GC allocation, or unhandled exception.
