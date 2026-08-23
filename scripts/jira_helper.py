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
BASE_URL = ENV.get("JIRA_BASE_URL", "https://buy2hrms.atlassian.net").rstrip("/")
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
        "Content-Type": "application/json"
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
    try:
        res = make_request(f"/rest/api/3/user/search?query={urllib.parse.quote(email)}")
        if isinstance(res, list) and len(res) > 0:
            return res[0].get("accountId")
    except Exception as e:
        print(f"[JIRA WARN] Could not resolve accountId for {email}: {e}", file=sys.stderr)
    return None

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

def fetch_next():
    os.makedirs(ARTIFACTS_DIR, exist_ok=True)
    artifact_path = os.path.join(ARTIFACTS_DIR, "00_jira_task.json")
    
    jql = urllib.parse.quote('status = "Backend To Do" ORDER BY created ASC')
    search_res = make_request(f"/rest/api/3/search?jql={jql}")
    
    issues = search_res.get("issues", [])
    if not issues:
        print("[JIRA] No tasks found with status 'Backend To Do'.")
        result = {"status": "NO_TASKS", "message": "No tasks in Backend To Do queue."}
        with open(artifact_path, "w", encoding="utf-8") as f:
            json.dump(result, f, indent=2)
        return result

    top_issue = issues[0]
    key = top_issue.get("key")
    fields = top_issue.get("fields", {})
    summary = fields.get("summary", "")
    raw_desc = fields.get("description", "")
    description = extract_text_from_description(raw_desc)
    
    print(f"[JIRA] Found task {key}: {summary}")
    
    # 1. Assign to engmohasami@gmail.com
    acc_id = get_account_id(EMAIL)
    if acc_id:
        try:
            make_request(f"/rest/api/3/issue/{key}/assignee", method="PUT", payload={"accountId": acc_id})
            print(f"[JIRA] Assigned {key} to account {acc_id}")
        except Exception as e:
            print(f"[JIRA WARN] Failed to assign issue: {e}", file=sys.stderr)

    # 2. Transition to "In Progress"
    try:
        trans_res = make_request(f"/rest/api/3/issue/{key}/transitions")
        transitions = trans_res.get("transitions", [])
        in_prog_trans = next((t for t in transitions if t.get("name", "").lower() == "in progress" or t.get("to", {}).get("name", "").lower() == "in progress"), None)
        if in_prog_trans:
            make_request(f"/rest/api/3/issue/{key}/transitions", method="POST", payload={"transition": {"id": in_prog_trans["id"]}})
            print(f"[JIRA] Transitioned {key} to 'In Progress'")
        else:
            print(f"[JIRA WARN] Transition 'In Progress' not found. Available: {[t.get('name') for t in transitions]}", file=sys.stderr)
    except Exception as e:
        print(f"[JIRA WARN] Failed transition: {e}", file=sys.stderr)

    # 3. Create branch name
    slug = slugify(summary)
    branch_name = f"feature/{key}-{slug}"
    
    result = {
        "status": "FETCHED",
        "key": key,
        "summary": summary,
        "description": description,
        "branch": branch_name,
        "jira_url": f"{BASE_URL}/browse/{key}"
    }
    
    with open(artifact_path, "w", encoding="utf-8") as f:
        json.dump(result, f, indent=2)

    print(f"[JIRA] Task artifact written to {artifact_path}")
    return result

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
        print(f"[JIRA WARN] Transition '{target_status}' not found. Available: {[t.get('name') for t in transitions]}", file=sys.stderr)

if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Usage: python jira_helper.py [fetch_next | comment_pr <key> <pr_url> | transition <key> <status>]")
        sys.exit(1)

    cmd = sys.argv[1]
    if cmd == "fetch_next":
        fetch_next()
    elif cmd == "comment_pr" and len(sys.argv) >= 4:
        comment_pr(sys.argv[2], sys.argv[3])
    elif cmd == "transition" and len(sys.argv) >= 4:
        transition_issue(sys.argv[2], sys.argv[3])
    else:
        print(f"Unknown command or invalid args: {cmd}")
        sys.exit(1)
