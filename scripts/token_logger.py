"""
token_logger.py — Phase 7b: PromptLayer Integration

Wraps jira_helper.log_gate_tokens() with PromptLayer remote tracking.
Enables team-wide token usage dashboard at app.promptlayer.com.

Usage:
    python scripts/token_logger.py log <gate_name> <tokens> <status>
    python scripts/token_logger.py summary        # Print per-gate summary for this task
    python scripts/token_logger.py dashboard      # Open PromptLayer dashboard URL

Config:
    Set PROMPTLAYER_API_KEY in .env
"""
import os
import sys
import json
import datetime

PROJECT_ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ARTIFACTS_DIR = os.path.join(PROJECT_ROOT, ".agent_artifacts")
CONTEXT_FILE = os.path.join(ARTIFACTS_DIR, "context.json")
TOKEN_LOG_FILE = os.path.join(ARTIFACTS_DIR, "token_log.json")


def _load_env() -> dict:
    env = {}
    env_path = os.path.join(PROJECT_ROOT, ".env")
    if os.path.exists(env_path):
        with open(env_path, encoding="utf-8") as f:
            for line in f:
                line = line.strip()
                if line and not line.startswith("#") and "=" in line:
                    k, v = line.split("=", 1)
                    env[k.strip()] = v.strip()
    return env


ENV = _load_env()
PROMPTLAYER_API_KEY = ENV.get("PROMPTLAYER_API_KEY", "")


def _load_context() -> dict:
    if not os.path.exists(CONTEXT_FILE):
        return {}
    with open(CONTEXT_FILE, encoding="utf-8") as f:
        return json.load(f)


def _load_token_log() -> list:
    if not os.path.exists(TOKEN_LOG_FILE):
        return []
    with open(TOKEN_LOG_FILE, encoding="utf-8") as f:
        try:
            return json.load(f)
        except json.JSONDecodeError:
            return []


def log_to_promptlayer(gate_name: str, tokens_used: int, status: str, jira_key: str):
    """Send gate token usage to PromptLayer for remote team-wide tracking."""
    if not PROMPTLAYER_API_KEY:
        print("[PROMPTLAYER] No API key in .env — logging locally only.")
        return

    try:
        import promptlayer
        pl_client = promptlayer.PromptLayer(api_key=PROMPTLAYER_API_KEY)

        # Log as a tracked request
        pl_client.track.request(
            provider_type="custom",
            request_params={
                "prompt": f"[{jira_key}] Gate: {gate_name}",
                "model": "pipeline-gate",
            },
            usage={
                "prompt_tokens": tokens_used,
                "completion_tokens": 0,
                "total_tokens": tokens_used,
            },
            tags=[jira_key, gate_name, status, "hr-pipeline"],
        )
        print(f"[PROMPTLAYER] Logged {tokens_used} tokens for gate '{gate_name}' on task {jira_key}.")
    except ImportError:
        print("[PROMPTLAYER] promptlayer package not installed. Run: pip install promptlayer")
    except Exception as e:
        print(f"[PROMPTLAYER] Logging failed: {e}", file=sys.stderr)


def log_gate(gate_name: str, tokens_used: int, status: str = "DONE"):
    """Log gate token usage both locally and to PromptLayer."""
    ctx = _load_context()
    jira_key = ctx.get("jira_key", "UNKNOWN")

    os.makedirs(ARTIFACTS_DIR, exist_ok=True)
    log = _load_token_log()
    log.append({
        "jira_key": jira_key,
        "gate": gate_name,
        "tokens_used": tokens_used,
        "status": status,
        "timestamp": datetime.datetime.utcnow().isoformat() + "Z",
    })
    with open(TOKEN_LOG_FILE, "w", encoding="utf-8") as f:
        json.dump(log, f, indent=2)

    log_to_promptlayer(gate_name, tokens_used, status, jira_key)
    print(f"[TOKEN LOG] {gate_name}: {tokens_used} tokens ({status})")


def print_summary():
    """Print per-gate token usage summary for the current task."""
    ctx = _load_context()
    jira_key = ctx.get("jira_key", "UNKNOWN")
    log = _load_token_log()
    task_entries = [e for e in log if e.get("jira_key") == jira_key]

    if not task_entries:
        print(f"No token log entries for task {jira_key}.")
        return

    total = sum(e.get("tokens_used", 0) for e in task_entries)
    print(f"\n{'─'*55}")
    print(f"  Token Usage Summary — {jira_key}")
    print(f"{'─'*55}")
    for e in task_entries:
        print(f"  {e['gate']:<30} {e['tokens_used']:>7} tokens  [{e['status']}]")
    print(f"{'─'*55}")
    print(f"  {'TOTAL':<30} {total:>7} tokens")
    print(f"{'─'*55}\n")


if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Usage: python token_logger.py [log <gate> <tokens> [status] | summary | dashboard]")
        sys.exit(1)

    cmd = sys.argv[1]
    if cmd == "log" and len(sys.argv) >= 4:
        status = sys.argv[4] if len(sys.argv) >= 5 else "DONE"
        log_gate(sys.argv[2], int(sys.argv[3]), status)
    elif cmd == "summary":
        print_summary()
    elif cmd == "dashboard":
        print("PromptLayer Dashboard: https://app.promptlayer.com/")
        print(f"Filter by tag: {_load_context().get('jira_key', 'UNKNOWN')}")
    else:
        print(f"Unknown command: {cmd}")
        sys.exit(1)
