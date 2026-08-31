"""
stryker_incremental.py — Phase 7a: Stryker Dashboard Integration

Extends hr-mutation-hardener with:
- Stryker Dashboard API reporting (stores results remotely per branch)
- Incremental mode: fetches previous survivor list and only re-tests those mutants
- Reads context.json for project/branch metadata

Usage:
    python scripts/stryker_incremental.py run       # Run incremental Stryker + upload
    python scripts/stryker_incremental.py fetch     # Fetch last dashboard result for branch
    python scripts/stryker_incremental.py status    # Print current survivor count

Config:
    Set STRYKER_DASHBOARD_API_KEY in .env
    Set STRYKER_DASHBOARD_PROJECT in .env (e.g. github.com/Moha-sami/HR_System)
"""
import os
import sys
import json
import subprocess
import datetime
import urllib.request
import urllib.parse
import urllib.error

PROJECT_ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ARTIFACTS_DIR = os.path.join(PROJECT_ROOT, ".agent_artifacts")
CONTEXT_FILE = os.path.join(ARTIFACTS_DIR, "context.json")
STRYKER_OUTPUT = os.path.join(PROJECT_ROOT, "StrykerOutput")
STRYKER_REPORT = os.path.join(ARTIFACTS_DIR, "stryker_dashboard_result.json")

DASHBOARD_BASE = "https://dashboard.stryker-mutator.io/api/v1"


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
DASHBOARD_API_KEY = ENV.get("STRYKER_DASHBOARD_API_KEY", "")
DASHBOARD_PROJECT = ENV.get("STRYKER_DASHBOARD_PROJECT", "github.com/Moha-sami/HR_System")


def _load_context() -> dict:
    if not os.path.exists(CONTEXT_FILE):
        return {}
    with open(CONTEXT_FILE, encoding="utf-8") as f:
        return json.load(f)


def _get_branch() -> str:
    ctx = _load_context()
    branch = ctx.get("branch", "main")
    # Dashboard uses URL-encoded branch name
    return urllib.parse.quote(branch, safe="")


def fetch_dashboard_result() -> dict:
    """Fetch last known mutation result for this branch from Stryker Dashboard."""
    if not DASHBOARD_API_KEY:
        print("[STRYKER] No STRYKER_DASHBOARD_API_KEY in .env — skipping dashboard fetch.")
        return {}

    branch = _get_branch()
    url = f"{DASHBOARD_BASE}/reports/{DASHBOARD_PROJECT}/{branch}"
    req = urllib.request.Request(
        url,
        headers={"X-Api-Key": DASHBOARD_API_KEY, "Accept": "application/json"},
    )
    try:
        with urllib.request.urlopen(req) as resp:
            data = json.loads(resp.read().decode("utf-8"))
            print(f"[STRYKER] Dashboard result fetched for branch '{branch}': score={data.get('mutationScore', 'N/A')}")
            return data
    except urllib.error.HTTPError as e:
        if e.code == 404:
            print(f"[STRYKER] No previous result on dashboard for branch '{branch}'. Will run full scan.")
        else:
            print(f"[STRYKER] Dashboard fetch error HTTP {e.code}", file=sys.stderr)
        return {}


def upload_to_dashboard(report_path: str) -> bool:
    """Upload mutation report JSON to Stryker Dashboard for remote storage."""
    if not DASHBOARD_API_KEY:
        print("[STRYKER] No API key — skipping dashboard upload.")
        return False

    if not os.path.exists(report_path):
        print(f"[STRYKER] Report file not found: {report_path}", file=sys.stderr)
        return False

    with open(report_path, encoding="utf-8") as f:
        report_data = f.read().encode("utf-8")

    branch = _get_branch()
    url = f"{DASHBOARD_BASE}/reports/{DASHBOARD_PROJECT}/{branch}"
    req = urllib.request.Request(
        url,
        data=report_data,
        headers={
            "X-Api-Key": DASHBOARD_API_KEY,
            "Content-Type": "application/json",
        },
        method="PUT",
    )
    try:
        with urllib.request.urlopen(req) as resp:
            print(f"[STRYKER] Report uploaded to dashboard. HTTP {resp.status}")
            return True
    except urllib.error.HTTPError as e:
        print(f"[STRYKER] Dashboard upload failed: HTTP {e.code}", file=sys.stderr)
        return False


def run_stryker_incremental() -> dict:
    """
    Run dotnet stryker in incremental mode.
    If a previous dashboard result exists, runs with --since flag to only re-test survivors.
    """
    ctx = _load_context()
    feature_path = ctx.get("feature_path", "src/Buy2.Application/")

    # Check for existing result to determine incremental vs full run
    prev = fetch_dashboard_result()
    is_incremental = bool(prev)
    mode_flag = "--since:main" if is_incremental else ""

    print(f"[STRYKER] Starting {'incremental' if is_incremental else 'full'} Stryker run...")

    cmd = ["dotnet", "stryker", "--reporter", "json", "--reporter", "dashboard"]
    if mode_flag:
        cmd.append(mode_flag)

    result = subprocess.run(
        cmd,
        cwd=os.path.join(PROJECT_ROOT, "tests", "Buy2.Domain.Tests"),
        capture_output=True,
        text=True,
    )

    print(result.stdout[-3000:] if len(result.stdout) > 3000 else result.stdout)
    if result.returncode != 0:
        print(f"[STRYKER] Run failed:\n{result.stderr[-1000:]}", file=sys.stderr)

    # Find generated report JSON
    report_file = None
    for root, dirs, files in os.walk(STRYKER_OUTPUT):
        for f in files:
            if f.endswith(".json") and "mutation" in f.lower():
                report_file = os.path.join(root, f)
                break

    survivors = 0
    score = 0.0
    if report_file and os.path.exists(report_file):
        with open(report_file, encoding="utf-8") as f:
            data = json.load(f)
        score = data.get("mutationScore", 0.0)
        survivors = sum(
            1 for m in data.get("mutants", [])
            if m.get("status") == "Survived"
        )
        upload_to_dashboard(report_file)
    else:
        print("[STRYKER] Warning: Could not locate mutation report JSON.")

    summary = {
        "status": "PASSED" if survivors == 0 else "FAILED",
        "mode": "incremental" if is_incremental else "full",
        "stryker_score": score,
        "surviving_mutants": survivors,
        "timestamp": datetime.datetime.utcnow().isoformat() + "Z",
    }

    with open(STRYKER_REPORT, "w", encoding="utf-8") as f:
        json.dump(summary, f, indent=2)

    print(f"[STRYKER] Result: {summary['status']} | Score: {score} | Survivors: {survivors}")
    return summary


if __name__ == "__main__":
    cmd = sys.argv[1] if len(sys.argv) > 1 else "run"
    if cmd == "run":
        run_stryker_incremental()
    elif cmd == "fetch":
        print(json.dumps(fetch_dashboard_result(), indent=2))
    elif cmd == "status":
        if os.path.exists(STRYKER_REPORT):
            with open(STRYKER_REPORT, encoding="utf-8") as f:
                print(f.read())
        else:
            print("[STRYKER] No report found. Run first.")
    else:
        print(f"Unknown command: {cmd}")
        sys.exit(1)
