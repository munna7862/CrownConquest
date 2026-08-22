---
name: doc-implementation-standards
description: Documentation standards for ChessForge architecture, chess-domain behavior, testing, UX, security and release evidence.
---

# Universal Documentation Implementation Standards for ChessForge

Every completed feature must be thoroughly documented in the repository **`docs/`** directory before a pull request can be merged or a sprint story closed.

---

### 1. Required Documentation by Change Type

Update the corresponding `docs/` subdirectories when making system modifications:

- **Architecture (`docs/architecture/` & `docs/adr/`):** Document high-level designs and create Architectural Decision Records (ADRs) for structural choices.
- **Chess Domain (`docs/chess/`):** Document supported FIDE rules, invariants, FEN/PGN codecs, and engine integration protocols.
- **Testing (`docs/testing/`):** Commit Test Cases Catalogs (`test_cases_catalog_P<XX>_S<YY>.md`) and regression test suites.
- **User Experience (`docs/ux/`):** Document user flows, board interaction specifications, keyboard shortcuts, and theme tokens.
- **Security & Desktop (`docs/security/`):** Document Tauri v2 capabilities, filesystem scopes, CSP policies, and dependency audit reports.
- **Release & Packaging (`docs/release/`):** Document installer builds, Windows packaging steps, checksums, and version changelogs.
- **Pull Requests (`docs/pull_requests/`):** Commit formal PR descriptions (`pr_P<XX>_S<YY>_<feature>.md`).

---

### 2. Mandatory Discipline: No Fake Artifacts

Do NOT generate irrelevant, placeholder, or fabricated documentation:

- Do not create HTTP API contracts for local desktop apps.
- Do not document database schemas when local JSON/file storage is used.
- Do not create `.env` documentation when no environment variables exist.
- Do not publish performance benchmarks or test reports without real, measured execution numbers.

---

### 3. Sprint Walkthrough (`walkthrough.md`)

Meaningful user-facing sprints must produce a concise `walkthrough.md` containing:

- **Feature Purpose:** What problem was solved.
- **Changed Behavior:** Clear breakdown of user-facing or architectural changes.
- **Verification Steps:** How to manually and automatically verify the changes.
- **Executed Tests:** Actual pass/fail results from local test runs.
- **Known Limitations:** Any edge cases deferred to subsequent sprints.

_(Do not claim screenshots or manual verification unless actually performed)._

---

### 4. Technical Best Practices

- **Language Tags:** Ensure all code blocks use precise language tags (`typescript`, `rust`, `json`, `bash`).
- **Visual Diagrams:** Embed native **Mermaid.js** diagrams for state machines, asynchronous worker flows, and architecture boundaries.
