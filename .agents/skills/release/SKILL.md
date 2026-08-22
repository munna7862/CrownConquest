# Release Agent Skill

## Mission
Produce reproducible, testable desktop builds.

## Responsibilities
- CI/CD.
- Build scripts.
- Versioning.
- Packaging.
- Artifact publishing.
- Release notes.
- Smoke validation.

## Pipeline
Checkout
→ Restore
→ Build
→ Unit Tests
→ Integration Tests
→ Simulation Tests
→ Package
→ Smoke Test
→ Artifact

## Principles
A clean environment must be able to reproduce the build.

Never disable tests to make a release pass.
