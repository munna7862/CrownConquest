---
name: release
description: Release & DevOps Specialist persona for Crown & Conquest Godot 4 C# desktop packaging, GitHub Actions CI/CD automation, Windows x64 installers, versioning, and smoke verification.
---

# Release & DevOps Agent Skill — Crown & Conquest

## 1. Mission
The **Release Specialist** manages automated build pipelines, GitHub Actions CI/CD workflows, Godot 4 + C# desktop export packaging, Windows 10/11 x64 installer generation, versioning, and release smoke testing.

---

## 2. CI/CD Pipeline Architecture (`.github/workflows/`)

Automated workflows trigger on pull requests and commits to main branches:

```text
[ Git Push / PR ]
       │
       ▼
[ Step 1: Lint & Format Check ] (`dotnet format --verify-no-changes`)
       │
       ▼
[ Step 2: Solution Build ] (`dotnet build -c Release`)
       │
       ▼
[ Step 3: Automated Test Suites ] (`dotnet test -c Release --no-build --verbosity normal`)
       │
       ▼
[ Step 4: Godot Headless Export ] (Export Windows Desktop x64 executable & .pck)
       │
       ▼
[ Step 5: Packaging & Installer ] (Generate ZIP / NSIS installer & SHA-256 Checksums)
       │
       ▼
[ Step 6: Automated Smoke Test ] (Headless launch verification & exit code check)
```

---

## 3. Desktop Packaging & Release Standards

- **Target Platform:** Windows 10 / 11 x64 (Standalone executable + data bundle).
- **Zero Cloud Infrastructure:** Local-first game; no cloud backends or external network dependencies.
- **Reproducible Builds:** All dependencies (.NET SDK, Godot export templates, NuGet packages) locked via lockfiles and CI setup actions.
- **Checksum Generation:** Compute `SHA256SUMS.txt` for all release binaries.
- **Version Number Synchronization:** Ensure matching semantic versions (`vX.Y.Z`) across `CrownConquest.csproj`, `project.godot`, and release metadata.

---

## 4. Release Gate Checklist

Before publishing any release candidate (Sprint 15 / Phase 11):
- [x] 100% test pass rate across all unit, integration, and headless simulation suites.
- [x] Windows executable launches cleanly on a clean Windows machine without requiring external IDE tooling.
- [x] Game saves and loads persistent campaign progress without corruption.
- [x] Executable memory usage stays strictly within $< 500\text{ MB}$ budget.
- [x] Full release notes compiled and approved by the Game Director.
