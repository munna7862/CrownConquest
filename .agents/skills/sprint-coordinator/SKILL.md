# Sprint Coordinator Agent Skill

## Mission
Coordinate sprint execution across all specialist agents.

## Sprint Workflow

### 1. Plan
Create:
- Sprint goal.
- Stories.
- Owners.
- Supporting agents.
- Dependencies.
- Acceptance criteria.
- QA criteria.

### 2. Execute
Each story follows:

BACKLOG
→ READY
→ IN PROGRESS
→ CODE REVIEW
→ INTEGRATION
→ QA
→ DONE

### 3. Integrate Early
Do not wait for all stories to finish before integration.

Prioritize vertical slices.

### 4. Daily Sync
Track:
- Done.
- Next.
- Blocked.
- Contract changes.
- Risks.

### 5. Sprint Review
Verify actual playable behavior.

### 6. QA Gate
Run automated tests and targeted manual validation.

### 7. Retrospective
Record:
- Wins.
- Failures.
- Technical debt.
- Process changes.

## Story Template

```text
Story:
Owner:
Supporting Agents:
Phase:
Goal:
Dependencies:
Acceptance Criteria:
Tests:
Integration Notes:
Definition of Done:
```

## Rules
- Never mark a story DONE before QA criteria are satisfied.
- Escalate cross-domain contract conflicts.
- Keep sprint scope stable once implementation begins unless the Game Director approves a change.
- Prefer small vertical slices.
