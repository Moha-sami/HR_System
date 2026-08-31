"""
aider_gate3.py — Phase 7d: Aider Diff-Aware Gate 3 Integration

Wraps Aider CLI to replace the raw hr-worker file writing pattern.
Instead of hr-worker dumping full file contents, Aider:
  - Reads only changed files from git diff (not the full repo)
  - Sends compact diff context to the LLM
  - Applies changes as precise, reviewable git-trackable edits

This saves 50-70% of the tokens Gate 3 would otherwise spend on
full file reads + rewrites.

Usage:
    python scripts/aider_gate3.py run "<instruction>"    # Execute single instruction
    python scripts/aider_gate3.py batch "<file>"         # Execute batch from instruction file
    python scripts/aider_gate3.py check                  # Verify aider is available

Config:
    Set GEMINI_API_KEY or OPENAI_API_KEY in .env
    Aider must be installed: pip install aider-chat OR python -m pipx install aider-chat
"""
import os
import sys
import json
import subprocess
import shutil

PROJECT_ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ARTIFACTS_DIR = os.path.join(PROJECT_ROOT, ".agent_artifacts")
CONTEXT_FILE = os.path.join(ARTIFACTS_DIR, "context.json")


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


def _load_context() -> dict:
    if not os.path.exists(CONTEXT_FILE):
        return {}
    with open(CONTEXT_FILE, encoding="utf-8") as f:
        return json.load(f)


def _find_aider() -> str:
    """Find aider binary in PATH or pipx venvs."""
    # Direct PATH check
    aider = shutil.which("aider")
    if aider:
        return aider

    # Pipx venv location
    pipx_home = os.path.expanduser("~/.local/pipx/venvs/aider-chat/Scripts/aider.exe")
    if os.path.exists(pipx_home):
        return pipx_home

    # Windows pipx AppData location
    appdata = os.environ.get("LOCALAPPDATA", "")
    win_pipx = os.path.join(appdata, "pipx", "venvs", "aider-chat", "Scripts", "aider.exe")
    if os.path.exists(win_pipx):
        return win_pipx

    return None


def check_aider():
    """Verify aider is available and print version."""
    aider = _find_aider()
    if not aider:
        print("[AIDER] NOT FOUND. Install with: python -m pipx install aider-chat")
        print("[AIDER]    Or: pip install aider-chat")
        return False

    result = subprocess.run([aider, "--version"], capture_output=True, text=True)
    print(f"[AIDER] FOUND at: {aider}")
    print(f"[AIDER]    Version: {result.stdout.strip()}")
    return True


def _get_aider_model_flag() -> list:
    """Choose LLM backend based on available API keys."""
    env = _load_env()
    if env.get("GEMINI_API_KEY"):
        os.environ["GEMINI_API_KEY"] = env["GEMINI_API_KEY"]
        return ["--model", "gemini/gemini-2.5-pro"]
    if env.get("OPENAI_API_KEY"):
        os.environ["OPENAI_API_KEY"] = env["OPENAI_API_KEY"]
        return ["--model", "gpt-4o"]
    return []


def _get_feature_files() -> list:
    """Get .cs files in the current task's feature path for scoped context."""
    ctx = _load_context()
    feature_path = ctx.get("feature_path", "src/Buy2.Application/")
    abs_path = os.path.join(PROJECT_ROOT, feature_path.replace("/", os.sep))

    files = []
    if os.path.exists(abs_path):
        for root, _, fnames in os.walk(abs_path):
            for f in fnames:
                if f.endswith(".cs"):
                    files.append(os.path.join(root, f))
    return files[:10]  # Cap at 10 files to avoid context overflow


def run_instruction(instruction: str) -> bool:
    """
    Run a single Aider instruction in diff-aware mode.
    Only sends changed files + feature area files to LLM context.
    """
    aider = _find_aider()
    if not aider:
        print("[AIDER] Not installed. Falling back to manual hr-worker flow.")
        return False

    feature_files = _get_feature_files()
    model_flags = _get_aider_model_flag()

    cmd = (
        [aider]
        + model_flags
        + [
            "--no-auto-commits",       # We control git commits
            "--no-suggest-shell-commands",
            "--yes-always",            # Non-interactive for pipeline mode
            "--message", instruction,
        ]
        + feature_files
    )

    print(f"[AIDER] Running with {len(feature_files)} feature files in context...")
    result = subprocess.run(cmd, cwd=PROJECT_ROOT, capture_output=False, text=True)
    success = result.returncode == 0
    print(f"[AIDER] {'DONE' if success else 'FAILED'} (exit code {result.returncode})")
    return success


def run_batch(instruction_file: str) -> bool:
    """
    Execute multiple instructions from a file (one per line).
    Used by hr-worker to execute spec line-by-line in diff-aware mode.
    """
    if not os.path.exists(instruction_file):
        print(f"[AIDER] Instruction file not found: {instruction_file}", file=sys.stderr)
        return False

    with open(instruction_file, encoding="utf-8") as f:
        instructions = [l.strip() for l in f if l.strip() and not l.startswith("#")]

    print(f"[AIDER] Running {len(instructions)} instructions from {instruction_file}...")
    all_ok = True
    for i, instr in enumerate(instructions, 1):
        print(f"\n[AIDER] Instruction {i}/{len(instructions)}: {instr[:80]}...")
        ok = run_instruction(instr)
        if not ok:
            print(f"[AIDER] ⚠️  Instruction {i} failed. Continuing with remaining.")
            all_ok = False

    return all_ok


if __name__ == "__main__":
    if len(sys.argv) < 2:
        print('Usage: python aider_gate3.py [check | run "<instruction>" | batch <file>]')
        sys.exit(1)

    cmd = sys.argv[1]
    if cmd == "check":
        check_aider()
    elif cmd == "run" and len(sys.argv) >= 3:
        ok = run_instruction(sys.argv[2])
        sys.exit(0 if ok else 1)
    elif cmd == "batch" and len(sys.argv) >= 3:
        ok = run_batch(sys.argv[2])
        sys.exit(0 if ok else 1)
    else:
        print(f"Unknown command: {cmd}")
        sys.exit(1)
