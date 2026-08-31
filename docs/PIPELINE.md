# 8-Gate Pipeline — Scripts Reference

## New Scripts (Phase 7)

| Script | Command | Purpose |
|---|---|---|
| `scripts/jira_helper.py` | `python scripts/jira_helper.py fetch SCRUM-XXX` | Step 0 — fetch task + build `context.json` |
| `scripts/pipeline_recovery.py` | `python scripts/pipeline_recovery.py check` | Check failed gates + get targeted fix instructions |
| `scripts/gate_planner.py` | `python scripts/gate_planner.py plan` | Print minimal gate execution plan for current change |
| `scripts/stryker_incremental.py` | `python scripts/stryker_incremental.py run` | Run Stryker in incremental mode + upload to dashboard |
| `scripts/token_logger.py` | `python scripts/token_logger.py summary` | Print per-gate token usage for current task |
| `scripts/cs_symbol_extractor.py` | `python scripts/cs_symbol_extractor.py diff` | Extract C# symbols from git-changed files |
| `scripts/aider_gate3.py` | `python scripts/aider_gate3.py check` | Verify Aider is installed for diff-aware Gate 3 |

## Required `.env` Keys

```
# Jira (already required)
JIRA_BASE_URL=https://buy-2hrms.atlassian.net
JIRA_EMAIL=engmohasami@gmail.com
JIRA_API_TOKEN=<your-token>

# Phase 7a — Stryker Dashboard (incremental mutation)
STRYKER_DASHBOARD_API_KEY=<from dashboard.stryker-mutator.io>
STRYKER_DASHBOARD_PROJECT=github.com/Moha-sami/HR_System

# Phase 7b — PromptLayer (team token usage dashboard)
PROMPTLAYER_API_KEY=<from app.promptlayer.com>

# Phase 7d — Aider / Gate Planner LLM backend
GEMINI_API_KEY=<your-gemini-key>
# OR
OPENAI_API_KEY=<your-openai-key>
```

## Recommended Startup Sequence (with all tools)

```powershell
# Start a task
python scripts/jira_helper.py fetch SCRUM-XXX
#   -> writes .agent_artifacts/context.json  (Step 0)
#   -> writes .agent_artifacts/00_jira_task.json

# Optional: see which gates need to run
python scripts/gate_planner.py plan

# Optional: pre-extract symbols for compact context
python scripts/cs_symbol_extractor.py diff

# After Gate 6 (mutation):
python scripts/stryker_incremental.py run
python scripts/jira_helper.py update_gate mutation_hardener PASS

# After pipeline completes:
python scripts/token_logger.py summary
```

## Aider Installation

`aider-chat` requires Python <= 3.12. If you're on 3.13, use a conda env:

```powershell
conda create -n aider python=3.12 -y
conda activate aider
pip install aider-chat
# Then set CONDA_AIDER_ENV=aider in .env so aider_gate3.py finds it
```

## Gate Plan Logic (rule-based, no LLM key needed)

| Condition | Gates Skipped |
|---|---|
| Docs/MD only change | Gates 2-7 (only Gate 1 + 8 run) |
| Only test files changed | Gates 2, 5, 6 |
| No handler/entity change | Gates 4 (reality-checker), 6 (mutation) |
| No auth/controller change | Gate 7b (security-reviewer) |
| Previous gate result = PASS, no src change | That gate skipped |
