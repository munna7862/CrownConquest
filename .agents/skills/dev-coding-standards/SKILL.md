---
name: dev-coding-standards
description: ChessForge production coding standards for TypeScript, React, Tauri, Rust boundaries, chess domain code, engine integration and persistence.
---

# Universal Dev Coding Standards for ChessForge

When writing production code for **ChessForge**, the following standards must be applied to guarantee 60fps UI performance, strict type safety, clean domain architecture, and crash-resilient desktop stability across **TypeScript (React/Vite)**, **Rust (Tauri v2)**, and **WebWorker (Stockfish WASM)**.

---

### 1. Strict Typing & Boundary Schema Validation

- **Zero Untyped Data (`any` strictly prohibited):**
  - In TypeScript: Run in `strict: true` mode. The `any` type is strictly forbidden; use `unknown` with explicit type narrowing guards (`typeof`, `instanceof`, or custom type predicates).
  - Prefer **discriminated unions** for state machines (game states, player turns, move types).
  - In Rust: Enforce strict type safety, exhaustiveness in `match` expressions, and avoid unchecked `unwrap()` in production code; use structured error handling (`Result<T, AppError>`).
- **Runtime Schema Validation at Boundaries:**
  - Validate all data crossing boundaries (Tauri IPC invocations, WebWorker messages, local storage / settings JSON, FEN/PGN string parsing) using **Zod** in TypeScript and **Serde** in Rust:

```typescript
import { z } from "zod";

export const MovePayloadSchema = z.object({
  from: z.string().regex(/^[a-h][1-8]$/),
  to: z.string().regex(/^[a-h][1-8]$/),
  promotion: z.enum(["q", "r", "b", "n"]).optional(),
});

export type MovePayload = z.infer<typeof MovePayloadSchema>;
```

---

### 2. Chess Domain Decoupling & Architecture

The chess domain is framework-independent. Never put chess legality checks inside React components.

```text
UI -> Application Service -> Chess Domain -> Chess Library Adapter
```

- **Domain Authority:** The domain owns legal move validation, board position, turn state, game status, move history, and FEN/PGN semantics.
- **Dependency Inversion:** Apply dependency inversion at meaningful boundaries:
  - Chess library adapter
  - Engine service
  - Persistence store
  - Native file APIs
  - Clock/timer source

---

### 3. Stockfish Engine Rules

Stockfish is an advisor, not the source of truth:

```text
Stockfish Engine -> proposed Move
Proposed Move -> Chess Domain validation
Domain -> commit or reject
```

- **Session & Request Identity:** All engine requests and responses must carry a request/session ID. Stale responses from previous positions or cancelled evaluations must be discarded immediately.

---

### 4. Tauri Native Rules

- Native Tauri commands must remain narrow and single-purpose.
- Never expose broad filesystem or arbitrary shell execution capabilities.
- Do not hide chess business logic inside Rust Tauri commands.
- Keep the frontend UI decoupled from Rust backend implementation details.
- Add a Tauri capability only when a concrete, active sprint requirement mandates it.

---

### 5. State Ownership & Single Source of Truth

- **Avoid Duplicate Authoritative State:** If the board position exists in the chess domain, do not maintain a second mutable board position in React.
- **State Taxonomy:**
  - **Domain State:** Authoritative game state (position, history, turn, clocks).
  - **Persistence State:** Serialized snapshot of game state and settings.
  - **Engine State:** Ephemeral evaluation metrics and proposed moves.
  - **UI State:** Transient presentation state (hovered square, piece drag coordinates, theme selection).

---

### 6. Error Handling & Async Safety

- Use typed domain and application errors. User-facing errors must be understandable without exposing raw stack traces.
- **Async Cleanup:** Always handle rejected promises, clean up WebWorker listeners, and cancel stale async operations on component unmount.
- **No Arbitrary Sleep:** Never use arbitrary timeout delays (`setTimeout`) as a synchronization mechanism.

---

### 7. Dependency Discipline

Before adding any npm package or cargo crate:

- Verify its license, maintenance status, bundle/runtime impact, and justify why built-in primitives or custom code are insufficient.
