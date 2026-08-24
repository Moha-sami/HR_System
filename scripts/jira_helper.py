import os
import sys
import json
import re
import base64
import urllib.request
import urllib.parse
import urllib.error

PROJECT_ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ARTIFACTS_DIR = os.path.join(PROJECT_ROOT, ".agent_artifacts")

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
        print("Usage: python jira_helper.py [fetch_next | fetch <KEY> | comment_pr <key> <pr_url> | transition <key> <status>]")
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
    else:
        print(f"Unknown command or invalid args: {cmd}")
        sys.exit(1)
