import os
import sys
import json
import re
import base64
import datetime
import urllib.request
import urllib.parse
import urllib.error

PROJECT_ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ARTIFACTS_DIR = os.path.join(PROJECT_ROOT, ".agent_artifacts")
CONTEXT_FILE = os.path.join(ARTIFACTS_DIR, "context.json")
TOKEN_LOG_FILE = os.path.join(ARTIFACTS_DIR, "token_log.json")
GRAFT_DIR = os.path.join(PROJECT_ROOT, ".graft")
ENTITIES_DIR = os.path.join(PROJECT_ROOT, "src", "Buy2.Domain", "Entities")

def load_env():
    env_vars = {}
    env_path = os.path.join(PROJECT_ROOT, ".env")
    if os.path.exists(env_path):
        with open(env_path, "r", encoding="utf-8") as f:
            for line in f:
                line = line.strip()
                if line and not line.startswith("#") and "=" in line:
                    k, v = line.split("=", 1)
                    env_vars[k.strip()] = v.strip()
    return env_vars

ENV = load_env()
BASE_URL = ENV.get("JIRA_BASE_URL", "https://buy-2hrms.atlassian.net").rstrip("/")
EMAIL = ENV.get("JIRA_EMAIL", "engmohasami@gmail.com")
API_TOKEN = ENV.get("JIRA_API_TOKEN", "")


# ---------------------------------------------------------------------------
# Context Bundle — Phase 1
# ---------------------------------------------------------------------------

def _read_arch_summary() -> str:
    """Read architecture_map.md and return a compact ~150-word digest."""
    arch_path = os.path.join(GRAFT_DIR, "architecture_map.md")
    if not os.path.exists(arch_path):
        return "Architecture map not found."
    with open(arch_path, "r", encoding="utf-8") as f:
        lines = f.readlines()
    # Keep first 40 non-empty lines as digest (avoids dumping full file into every gate)
    digest = [l.rstrip() for l in lines if l.strip()][:40]
    return "\n".join(digest)


def _list_existing_entities() -> list:
    """Return sorted list of existing domain entity names (without .cs extension)."""
    if not os.path.exists(ENTITIES_DIR):
        return []
    return sorted(
        f.replace(".cs", "")
        for f in os.listdir(ENTITIES_DIR)
        if f.endswith(".cs") and f != "BaseEntity.cs"
    )


def build_context_bundle(key: str, summary: str, description: str, branch: str) -> dict:
    """Compile shared context.json at Step 0. All gates read this instead of raw files."""
    os.makedirs(ARTIFACTS_DIR, exist_ok=True)

    # Infer feature area from summary keywords
    feature_keywords = {
        "point": "Points", "role": "Roles", "job": "Jobs", "employee": "Employees",
        "department": "Departments", "qualification": "Qualifications",
        "shift": "ShiftMarket", "site": "Sites", "auth": "Authentication",
        "payroll": "Payroll", "attendance": "Attendance",
    }
    feature_area = "General"
    for kw, area in feature_keywords.items():
        if kw in summary.lower() or kw in description.lower():
            feature_area = area
            break

    bundle = {
        "jira_key": key,
        "title": summary,
        "description": description,
        "branch": branch,
        "jira_url": f"{BASE_URL}/browse/{key}",
        "feature_area": feature_area,
        "feature_path": f"src/Buy2.Application/Features/{feature_area}/",
        "existing_entities": _list_existing_entities(),
        "rbac_roles": ["Admin", "SuperAdmin", "HR", "Manager", "Employee"],
        "persistence_boundary": "IRepository<T> + IUnitOfWork only. Never inject DbContext into Application layer.",
        "cqrs_pattern": "Co-located Command/Query record + Handler in same .cs file under Features/<Area>/",
        "cc_limit": "Cyclomatic Complexity <= 6 per method, target <= 4.",
        "arch_summary": _read_arch_summary(),
        "created_at": datetime.datetime.utcnow().isoformat() + "Z",
        "gate_results": {},
        "recovery_attempts": 0,
    }

    with open(CONTEXT_FILE, "w", encoding="utf-8") as f:
        json.dump(bundle, f, indent=2)

    print(f"[PIPELINE] context.json written to {CONTEXT_FILE}")
    return bundle


# ---------------------------------------------------------------------------
# Gate Result Tracking — Phase 2
# ---------------------------------------------------------------------------

def read_context() -> dict:
    """Load context.json. Returns empty dict if not found."""
    if not os.path.exists(CONTEXT_FILE):
        print(f"[PIPELINE WARNING] context.json not found at {CONTEXT_FILE}", file=sys.stderr)
        return {}
    with open(CONTEXT_FILE, "r", encoding="utf-8") as f:
        return json.load(f)


def update_gate_result(gate_name: str, status: str, details: dict = None):
    """Write a gate pass/fail result into context.json gate_results."""
    ctx = read_context()
    if not ctx:
        return
    ctx.setdefault("gate_results", {})[gate_name] = {
        "status": status,
        "timestamp": datetime.datetime.utcnow().isoformat() + "Z",
        "details": details or {},
    }
    with open(CONTEXT_FILE, "w", encoding="utf-8") as f:
        json.dump(ctx, f, indent=2)
    print(f"[PIPELINE] Gate '{gate_name}' result written: {status}")


# ---------------------------------------------------------------------------
# Token Usage Logging — Phase 5
# ---------------------------------------------------------------------------

def log_gate_tokens(gate_name: str, tokens_used: int, status: str = "DONE"):
    """Append per-gate token usage to token_log.json."""
    os.makedirs(ARTIFACTS_DIR, exist_ok=True)
    log = []
    if os.path.exists(TOKEN_LOG_FILE):
        with open(TOKEN_LOG_FILE, "r", encoding="utf-8") as f:
            try:
                log = json.load(f)
            except json.JSONDecodeError:
                log = []

    ctx = read_context()
    log.append({
        "jira_key": ctx.get("jira_key", "UNKNOWN"),
        "gate": gate_name,
        "tokens_used": tokens_used,
        "status": status,
        "timestamp": datetime.datetime.utcnow().isoformat() + "Z",
    })

    with open(TOKEN_LOG_FILE, "w", encoding="utf-8") as f:
        json.dump(log, f, indent=2)
    print(f"[PIPELINE] Token log updated: {gate_name} used {tokens_used} tokens.")


def get_auth_header():
    raw = f"{EMAIL}:{API_TOKEN}".encode("utf-8")
    return f"Basic {base64.b64encode(raw).decode('utf-8')}"

def make_request(path, method="GET", payload=None):
    url = f"{BASE_URL}{path}"
    headers = {
        "Authorization": get_auth_header(),
        "Accept": "application/json",
        "Content-Type": "application/json",
        "User-Agent": "AntigravityJiraHelper/1.0"
    }
    data = json.dumps(payload).encode("utf-8") if payload is not None else None
    req = urllib.request.Request(url, data=data, headers=headers, method=method)
    
    try:
        with urllib.request.urlopen(req) as resp:
            body = resp.read().decode("utf-8")
            return json.loads(body) if body else {}
    except urllib.error.HTTPError as e:
        err_body = e.read().decode("utf-8")
        print(f"[JIRA ERROR] HTTP {e.code} on {method} {path}: {err_body}", file=sys.stderr)
        raise

def get_account_id(email=EMAIL):
    res = make_request(f"/rest/api/3/user/search?query={urllib.parse.quote(email)}")
    if isinstance(res, list) and len(res) > 0:
        acc_id = res[0].get("accountId")
        if acc_id:
            return acc_id
    print(f"[JIRA FATAL ERROR] Could not resolve accountId for {email}", file=sys.stderr)
    sys.exit(1)

def slugify(text):
    text = text.lower()
    text = re.sub(r'[^a-z0-9]+', '-', text)
    return text.strip('-')[:50]

def extract_text_from_description(desc):
    if not desc:
        return ""
    if isinstance(desc, str):
        return desc
    if isinstance(desc, dict):
        text_parts = []
        def _recurse(node):
            if isinstance(node, dict):
                if node.get("type") == "text":
                    text_parts.append(node.get("text", ""))
                for v in node.values():
                    _recurse(v)
            elif isinstance(node, list):
                for item in node:
                    _recurse(item)
        _recurse(desc)
        return " ".join(text_parts)
    return str(desc)

def process_and_update_issue(key, summary, description):
    os.makedirs(ARTIFACTS_DIR, exist_ok=True)
    artifact_path = os.path.join(ARTIFACTS_DIR, "00_jira_task.json")
    
    # 1. Fetch accountId & assign issue
    try:
        acc_id = get_account_id(EMAIL)
        make_request(f"/rest/api/3/issue/{key}/assignee", method="PUT", payload={"accountId": acc_id})
        print(f"[JIRA] Successfully assigned {key} to account {acc_id} ({EMAIL})")
        jira_assigned = True
    except Exception as e:
        print(f"[JIRA FATAL ERROR] Failed to assign issue {key}: {e}", file=sys.stderr)
        sys.exit(1)

    # 2. Transition issue to "In Progress"
    try:
        trans_res = make_request(f"/rest/api/3/issue/{key}/transitions")
        transitions = trans_res.get("transitions", [])
        in_prog_trans = next((t for t in transitions if t.get("name", "").lower() == "in progress" or t.get("to", {}).get("name", "").lower() == "in progress"), None)
        if not in_prog_trans:
            print(f"[JIRA FATAL ERROR] Transition 'In Progress' not available for {key}. Available: {[t.get('name') for t in transitions]}", file=sys.stderr)
            sys.exit(1)
            
        make_request(f"/rest/api/3/issue/{key}/transitions", method="POST", payload={"transition": {"id": in_prog_trans["id"]}})
        print(f"[JIRA] Successfully transitioned {key} to 'In Progress'")
        jira_status_updated = True
    except Exception as e:
        print(f"[JIRA FATAL ERROR] Failed to transition issue {key}: {e}", file=sys.stderr)
        sys.exit(1)

    slug = slugify(summary)
    branch_name = f"feature/{key}-{slug}"
    
    result = {
        "status": "FETCHED",
        "key": key,
        "summary": summary,
        "description": description,
        "branch": branch_name,
        "jira_assigned": jira_assigned,
        "jira_status_updated": jira_status_updated,
        "jira_url": f"{BASE_URL}/browse/{key}"
    }
    
    with open(artifact_path, "w", encoding="utf-8") as f:
        json.dump(result, f, indent=2)

    print(f"[JIRA] Task artifact successfully written to {artifact_path}")
    print(f"[JIRA] Branch: {branch_name}")

    # Phase 1 — compile shared context bundle for all downstream gates
    build_context_bundle(key, summary, description, branch_name)

    return result

def fetch_next():
    try:
        jql = urllib.parse.quote('status = "Backend To Do" ORDER BY created ASC')
        search_res = make_request(f"/rest/api/3/search?jql={jql}")
        issues = search_res.get("issues", [])
    except Exception as e:
        print(f"[JIRA FATAL ERROR] Could not query Jira API: {e}", file=sys.stderr)
        sys.exit(1)

    if not issues:
        print("[JIRA FATAL ERROR] No tasks found in 'Backend To Do' queue.", file=sys.stderr)
        sys.exit(1)

    top_issue = issues[0]
    key = top_issue.get("key")
    fields = top_issue.get("fields", {})
    summary = fields.get("summary", "")
    raw_desc = fields.get("description", "")
    description = extract_text_from_description(raw_desc)
    
    return process_and_update_issue(key, summary, description)

def fetch_issue(key):
    try:
        issue = make_request(f"/rest/api/3/issue/{key}")
        fields = issue.get("fields", {})
        summary = fields.get("summary", "")
        raw_desc = fields.get("description", "")
        description = extract_text_from_description(raw_desc)
    except Exception as e:
        print(f"[JIRA FATAL ERROR] Could not fetch issue {key}: {e}", file=sys.stderr)
        sys.exit(1)

    return process_and_update_issue(key, summary, description)

def comment_pr(key, pr_url):
    comment_body = {
        "body": {
            "type": "doc",
            "version": 1,
            "content": [
                {
                    "type": "paragraph",
                    "content": [
                        {"type": "text", "text": "Pull Request created for this task: "},
                        {
                            "type": "text",
                            "text": pr_url,
                            "marks": [{"type": "link", "attrs": {"href": pr_url}}]
                        }
                    ]
                }
            ]
        }
    }
    make_request(f"/rest/api/3/issue/{key}/comment", method="POST", payload=comment_body)
    print(f"[JIRA] Posted PR comment to {key}")

def transition_issue(key, target_status):
    trans_res = make_request(f"/rest/api/3/issue/{key}/transitions")
    transitions = trans_res.get("transitions", [])
    t_match = next((t for t in transitions if t.get("name", "").lower() == target_status.lower() or t.get("to", {}).get("name", "").lower() == target_status.lower()), None)
    if t_match:
        make_request(f"/rest/api/3/issue/{key}/transitions", method="POST", payload={"transition": {"id": t_match["id"]}})
        print(f"[JIRA] Transitioned {key} to '{target_status}'")
    else:
        print(f"[JIRA ERROR] Transition '{target_status}' not found. Available: {[t.get('name') for t in transitions]}", file=sys.stderr)
        sys.exit(1)

if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Usage: python jira_helper.py [fetch_next | fetch <KEY> | comment_pr <key> <pr_url> | transition <key> <status> | update_gate <gate_name> <PASS|FAIL> | log_tokens <gate_name> <count> | read_context]")
        sys.exit(1)

    cmd = sys.argv[1]
    if cmd == "fetch_next":
        fetch_next()
    elif cmd == "fetch" and len(sys.argv) >= 3:
        fetch_issue(sys.argv[2])
    elif cmd == "comment_pr" and len(sys.argv) >= 4:
        comment_pr(sys.argv[2], sys.argv[3])
    elif cmd == "transition" and len(sys.argv) >= 4:
        transition_issue(sys.argv[2], sys.argv[3])
    elif cmd == "update_gate" and len(sys.argv) >= 4:
        # Usage: python jira_helper.py update_gate spec_brainstorm PASS
        details = {"raw": sys.argv[4]} if len(sys.argv) >= 5 else {}
        update_gate_result(sys.argv[2], sys.argv[3], details)
    elif cmd == "log_tokens" and len(sys.argv) >= 4:
        # Usage: python jira_helper.py log_tokens hr-worker 3400
        log_gate_tokens(sys.argv[2], int(sys.argv[3]))
    elif cmd == "read_context":
        ctx = read_context()
        print(json.dumps(ctx, indent=2))
    else:
        print(f"Unknown command or invalid args: {cmd}")
        sys.exit(1)
