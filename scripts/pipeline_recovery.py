"""
pipeline_recovery.py — Phase 4: Auto-Recovery Loop

Reads context.json to detect failed gates (mutation or test) and
determines which gates need re-run. Caps retries at MAX_RETRIES=2.

Usage:
    python scripts/pipeline_recovery.py check          # Print failed gates
    python scripts/pipeline_recovery.py increment      # Increment recovery_attempts counter
    python scripts/pipeline_recovery.py reset          # Reset recovery_attempts to 0
"""
import os
import sys
import json
import datetime

PROJECT_ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ARTIFACTS_DIR = os.path.join(PROJECT_ROOT, ".agent_artifacts")
CONTEXT_FILE = os.path.join(ARTIFACTS_DIR, "context.json")

MAX_RETRIES = 2

RECOVERABLE_GATES = ["hr-mutation-hardener", "hr-test-reviewer"]
RECOVERY_CHAIN = ["hr-worker", "hr-mutation-hardener", "hr-test-reviewer"]


def _load_context() -> dict:
    if not os.path.exists(CONTEXT_FILE):
        print("[RECOVERY ERROR] context.json not found.", file=sys.stderr)
        sys.exit(1)
    with open(CONTEXT_FILE, "r", encoding="utf-8") as f:
        return json.load(f)


def _save_context(ctx: dict):
    with open(CONTEXT_FILE, "w", encoding="utf-8") as f:
        json.dump(ctx, f, indent=2)


def check_failed_gates() -> list:
    """Return list of failed gate names from gate_results."""
    ctx = _load_context()
    gate_results = ctx.get("gate_results", {})
    failed = [
        gate for gate, result in gate_results.items()
        if result.get("status") in ("FAIL", "BLOCKED") and gate in RECOVERABLE_GATES
    ]
    return failed


def get_recovery_instructions() -> dict:
    """
    Returns recovery plan:
    - which gates to re-run
    - exact failing details (surviving mutants / failing tests)
    - whether we've hit MAX_RETRIES
    """
    ctx = _load_context()
    attempts = ctx.get("recovery_attempts", 0)
    failed = check_failed_gates()
    jira_key = ctx.get("jira_key", "UNKNOWN")

    if not failed:
        return {"status": "ALL_PASSING", "re_run_gates": [], "attempts": attempts}

    if attempts >= MAX_RETRIES:
        return {
            "status": "MAX_RETRIES_EXCEEDED",
            "message": (
                f"[PIPELINE ALERT] {jira_key}: Recovery loop hit MAX_RETRIES ({MAX_RETRIES}). "
                f"Human intervention required. Failed gates: {failed}. "
                f"Review .agent_artifacts/context.json gate_results for details."
            ),
            "failed_gates": failed,
            "re_run_gates": [],
            "attempts": attempts,
        }

    # Build targeted fix instruction from gate details
    fix_details = {}
    for gate in failed:
        gate_result = ctx.get("gate_results", {}).get(gate, {})
        fix_details[gate] = gate_result.get("details", {})

    return {
        "status": "RECOVERY_NEEDED",
        "jira_key": jira_key,
        "failed_gates": failed,
        "re_run_gates": RECOVERY_CHAIN,
        "fix_details": fix_details,
        "attempts": attempts,
        "instruction": (
            f"Send targeted fix instructions to hr-worker with the following failing details: "
            f"{json.dumps(fix_details, indent=2)}. "
            f"Then re-run gates: {', '.join(RECOVERY_CHAIN)} only."
        ),
    }


def increment_attempts():
    """Increment recovery_attempts counter in context.json."""
    ctx = _load_context()
    ctx["recovery_attempts"] = ctx.get("recovery_attempts", 0) + 1
    ctx["last_recovery_at"] = datetime.datetime.utcnow().isoformat() + "Z"
    _save_context(ctx)
    print(f"[RECOVERY] Attempt counter incremented to {ctx['recovery_attempts']} / {MAX_RETRIES}")


def reset_attempts():
    """Reset recovery_attempts counter after successful recovery."""
    ctx = _load_context()
    ctx["recovery_attempts"] = 0
    _save_context(ctx)
    print("[RECOVERY] Attempt counter reset to 0.")


if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Usage: python pipeline_recovery.py [check | increment | reset]")
        sys.exit(1)

    cmd = sys.argv[1]
    if cmd == "check":
        plan = get_recovery_instructions()
        print(json.dumps(plan, indent=2))
    elif cmd == "increment":
        increment_attempts()
    elif cmd == "reset":
        reset_attempts()
    else:
        print(f"Unknown command: {cmd}")
        sys.exit(1)
