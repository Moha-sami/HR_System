# OctaFlow Agent Rules & Workflow Protocol

This project uses **OctaFlow** for token-efficient, quality-gated software engineering.
All AI assistants (Claude Code, Antigravity, Cursor, Roo-Code) **MUST** strictly adhere to the following workflow when working on any task or issue.

---

## ⪡ Core Rules (Non-Negotiable)

1. **Dedicated Feature Branch for Every Task**:
   - **Never** develop or commit directly on `main` or `master`.
   - Always ensure a task-specific feature branch exists and is checked out before touching any code:
     ```bash
     git checkout -b feature/<TICKET-KEY>
     ```
   - Running `octaflow start <TICKET-KEY>` handles branch verification and checkout automatically.

2. **Subagent Context Isolation**:
   - Never run massive compilation loops, test runners, or full file scans directly in the primary conversation.
   - Spawn a dedicated subagent (`invoke_subagent` / background agent) to perform the implementation, tests, and debugging.
   - The primary agent coordinates: gathers requirements, reviews the implementation plan, invokes the subagent, and verifies results.

3. **Step 0: Context Compilation**:
   - Before editing code, run:
     ```bash
     octaflow start <TICKET-KEY>
     ```
   - This compiles `.agent_artifacts/context.json` and evaluates the AI Dynamic Gate Plan.
   - Read `.agent_artifacts/context.json` to obtain project conventions and commands. Do NOT scan the entire codebase manually.

4. **TDD Implementation & Wave Execution**:
   - Execute gates according to the dependency graph:
     - Find edge cases before coding (`spec-brainstormer`)
     - Verify layer boundaries (`architecture-guardian`)
     - TDD Red-Green-Refactor (`worker`)
     - Parallel validation (`reality-checker` + `performance-sentinel` + `code-cleaner`)
     - 0 surviving mutants (`mutation-hardener`)
     - Parallel security & QA (`test-reviewer` + `security-reviewer`)
     - PR handoff with metrics badge (`docs-agent`)

5. **Zero Cyclomatic Complexity Bloat**:
   - All new methods must maintain Cyclomatic Complexity <= 6 (target <= 4).
   - Run `symbol_extractor` static AST checks before committing.

6. **Telemetry & Verification**:
   - Ensure all unit tests pass (100% pass rate).
   - Log gate status via `octaflow log <gate-id> <tokens> --status PASS`.
   - Update `.agent_artifacts/PR_SUMMARY.md` with the OctaFlow verification badge before raising a PR.

7. **Mandatory Automated Pull Request Handoff**:
   - When implementation and test verification are complete:
     ```bash
     octaflow pr
     ```
   - **PR Title (Strict Convention)**:
     Format: `<SCRUM_NUMBER>: <Ticket Title>`
     Example: `SCRUM-342: Shift Employee Candidate Pool Search & Profile Preview (Backend)`
   - **PR Description (Strict Convention)**:
     Must include a short, clear description of what was done:
     - `## Description`: 2-3 sentence overview of what was implemented or resolved.
     - `### What was done`: Concise bullet points of all endpoints, handlers, entities, and tests added.
     - `### Verification`: Confirmation of 100% test pass rate and 0 compiler warnings.
     - `### ⚡ OctaFlow Verification Report`: Token and gate metrics table.
   - `octaflow pr` automatically synchronizes the feature branch to remote (`git push -u origin <branch>`), opens the PR via `gh pr create` with `.agent_artifacts/PR_SUMMARY.md`, records `.agent_artifacts/04_git_pr.json`, and outputs the clickable PR URL.
   - **Never** mark a task as completed without creating the PR and returning the PR link to the user.

