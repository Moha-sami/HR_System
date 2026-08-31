"""
gate_planner.py — Phase 7e: Semantic Kernel Dynamic Gate Planner

Instead of always running all 8 gates, this planner analyzes:
- The Jira task description and scope
- Which files changed (git diff)
- Previous gate_results in context.json

And produces a minimal ordered list of gates that ACTUALLY need to run.

Examples:
- Docs-only change → skip Gates 3-7, run only Gate 8
- Only validator changed → skip Gate 3 rewrite, run Gates 5-7
- New entity added → run all gates

Usage:
    python scripts/gate_planner.py plan          # Print recommended gate execution order
    python scripts/gate_planner.py explain       # Explain why each gate was included/skipped

Config:
    Set GEMINI_API_KEY or OPENAI_API_KEY in .env for LLM-backed planning.
    Falls back to deterministic rule-based planning if no LLM key present.
"""
import os
import sys
import json
import subprocess
import datetime

PROJECT_ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ARTIFACTS_DIR = os.path.join(PROJECT_ROOT, ".agent_artifacts")
CONTEXT_FILE = os.path.join(ARTIFACTS_DIR, "context.json")
PLAN_FILE = os.path.join(ARTIFACTS_DIR, "gate_plan.json")


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


def _get_changed_files() -> list:
    result = subprocess.run(
        ["git", "diff", "--name-only", "HEAD"],
        cwd=PROJECT_ROOT, capture_output=True, text=True,
    )
    return [f.strip() for f in result.stdout.splitlines() if f.strip()]


ALL_GATES = [
    "hr-spec-brainstormer",
    "hr-architecture-guardian",
    "hr-worker",
    "hr-reality-checker",
    "hr-code-cleaner",
    "hr-mutation-hardener",
    "hr-test-reviewer",
    "hr-security-reviewer",
    "hr-docs-agent",
]


def _rule_based_plan(ctx: dict, changed_files: list) -> dict:
    """
    Deterministic rule-based gate planner — no LLM needed.
    Analyses changed file paths and context to determine minimum gate set.
    """
    gate_results = ctx.get("gate_results", {})
    skip_reasons = {}
    run_gates = []

    cs_files = [f for f in changed_files if f.endswith(".cs")]
    test_files = [f for f in cs_files if "Tests" in f or "tests" in f]
    src_files = [f for f in cs_files if "Tests" not in f and "tests" not in f]
    md_only = all(f.endswith(".md") or f.endswith(".json") for f in changed_files) if changed_files else False
    has_new_entity = any("Domain/Entities" in f for f in src_files)
    has_handler = any("Features" in f and "Handler" in f for f in src_files)
    has_controller = any("Controller" in f for f in src_files)
    has_validator = any("Validator" in f for f in src_files)
    docs_only = md_only or (not cs_files and changed_files)

    # Gate 1 — always run unless already passed with no spec change
    if gate_results.get("spec_brainstorm", {}).get("status") == "PASS" and not src_files:
        skip_reasons["hr-spec-brainstormer"] = "No source changes; previous spec still valid."
    else:
        run_gates.append("hr-spec-brainstormer")

    # Gate 2 — skip if only test files changed (no arch implications)
    if test_files and not src_files:
        skip_reasons["hr-architecture-guardian"] = "Only test files changed; no architecture review needed."
    elif docs_only:
        skip_reasons["hr-architecture-guardian"] = "Docs/config only change."
    else:
        run_gates.append("hr-architecture-guardian")

    # Gate 3 — skip if only docs or tests changed
    if docs_only or (test_files and not src_files):
        skip_reasons["hr-worker"] = "No src changes requiring implementation work."
    else:
        run_gates.append("hr-worker")

    # Gate 4 (reality-checker) — run if handler or entity changed
    if has_handler or has_new_entity:
        run_gates.append("hr-reality-checker")
    else:
        skip_reasons["hr-reality-checker"] = "No handler or entity changes detected."

    # Gate 5 (code-cleaner) — skip if already passing and no src changed
    if gate_results.get("code_cleaner", {}).get("status") == "PASS" and not src_files:
        skip_reasons["hr-code-cleaner"] = "CC already clean; no src changes."
    elif docs_only:
        skip_reasons["hr-code-cleaner"] = "Docs only change."
    else:
        run_gates.append("hr-code-cleaner")

    # Gate 6 (mutation) — skip if only validators/DTOs changed (not handlers)
    if not has_handler and not has_new_entity:
        skip_reasons["hr-mutation-hardener"] = "No handler/entity changes — mutation testing skipped."
    elif gate_results.get("mutation_hardener", {}).get("status") == "PASS" and not src_files:
        skip_reasons["hr-mutation-hardener"] = "Already passing; no new src changes."
    elif docs_only:
        skip_reasons["hr-mutation-hardener"] = "Docs only change."
    else:
        run_gates.append("hr-mutation-hardener")

    # Gate 7a (test reviewer) — skip if only docs changed
    if docs_only:
        skip_reasons["hr-test-reviewer"] = "Docs only change."
    else:
        run_gates.append("hr-test-reviewer")

    # Gate 7b (security reviewer) — skip if no controller/auth files changed
    auth_relevant = any(("Auth" in f or "Controller" in f or "Permission" in f) for f in src_files)
    if not auth_relevant and not has_new_entity:
        skip_reasons["hr-security-reviewer"] = "No auth/controller/entity changes."
    elif docs_only:
        skip_reasons["hr-security-reviewer"] = "Docs only change."
    else:
        run_gates.append("hr-security-reviewer")

    # Gate 8 (docs) — always run
    run_gates.append("hr-docs-agent")

    return {
        "planner": "rule-based",
        "jira_key": ctx.get("jira_key", "UNKNOWN"),
        "gates_to_run": run_gates,
        "gates_skipped": skip_reasons,
        "total_gates": len(ALL_GATES),
        "running_gates": len(run_gates),
        "skipped_gates": len(skip_reasons),
        "estimated_savings_pct": round(len(skip_reasons) / len(ALL_GATES) * 100),
        "changed_files": changed_files,
        "generated_at": datetime.datetime.utcnow().isoformat() + "Z",
    }


def _llm_backed_plan(ctx: dict, changed_files: list) -> dict:
    """
    LLM-backed planning using Semantic Kernel.
    Generates a natural language explanation + gate list for complex changes.
    """
    env = _load_env()
    api_key = env.get("GEMINI_API_KEY") or env.get("OPENAI_API_KEY")

    if not api_key:
        print("[PLANNER] No LLM API key found — using rule-based planner.")
        return _rule_based_plan(ctx, changed_files)

    try:
        import semantic_kernel as sk
        from semantic_kernel.connectors.ai.open_ai import OpenAIChatCompletion
        from semantic_kernel.prompt_template import PromptTemplateConfig

        kernel = sk.Kernel()

        # Register OpenAI/Gemini service
        service = OpenAIChatCompletion(
            ai_model_id="gpt-4o-mini",
            api_key=api_key,
        )
        kernel.add_service(service)

        prompt = f"""
You are a CI/CD gate planner for a .NET CQRS backend project.

Available gates (in order):
{json.dumps(ALL_GATES, indent=2)}

Jira task: {ctx.get('title', 'N/A')}
Description: {ctx.get('description', 'N/A')[:500]}
Feature area: {ctx.get('feature_area', 'N/A')}
Changed files: {json.dumps(changed_files)}
Previous gate results: {json.dumps(ctx.get('gate_results', {}), indent=2)}

Which gates MUST run? Which can be safely skipped?
Return JSON with keys: gates_to_run (list), gates_skipped (dict of gate->reason).
Return ONLY the JSON object, no extra text.
"""
        import asyncio

        async def _run():
            result = await kernel.invoke_prompt(prompt)
            return str(result)

        raw = asyncio.run(_run())
        parsed = json.loads(raw.strip())
        parsed["planner"] = "semantic-kernel-llm"
        parsed["jira_key"] = ctx.get("jira_key", "UNKNOWN")
        parsed["generated_at"] = datetime.datetime.utcnow().isoformat() + "Z"
        return parsed

    except Exception as e:
        print(f"[PLANNER] LLM planning failed ({e}), falling back to rule-based.", file=sys.stderr)
        return _rule_based_plan(ctx, changed_files)


def generate_plan(use_llm: bool = True) -> dict:
    ctx = _load_context()
    changed = _get_changed_files()

    if use_llm:
        plan = _llm_backed_plan(ctx, changed)
    else:
        plan = _rule_based_plan(ctx, changed)

    os.makedirs(ARTIFACTS_DIR, exist_ok=True)
    with open(PLAN_FILE, "w", encoding="utf-8") as f:
        json.dump(plan, f, indent=2)

    return plan


def print_plan():
    plan = generate_plan()
    print(f"\n{'='*60}")
    print(f"  Gate Plan -- {plan.get('jira_key')}  [{plan['planner']}]")
    print(f"{'='*60}")
    print(f"\n  [RUN] Gates to run ({plan['running_gates']}/{plan['total_gates']}):")
    for g in plan.get("gates_to_run", []):
        print(f"     -> {g}")
    if plan.get("gates_skipped"):
        print(f"\n  [SKIP] Gates skipped ({plan['skipped_gates']}):")
        for g, reason in plan.get("gates_skipped", {}).items():
            print(f"     x {g}: {reason}")
    print(f"\n  Estimated token savings: ~{plan['estimated_savings_pct']}%")
    print(f"{'='*60}\n")
    return plan


if __name__ == "__main__":
    cmd = sys.argv[1] if len(sys.argv) > 1 else "plan"
    use_llm = "--no-llm" not in sys.argv

    if cmd == "plan":
        print_plan()
    elif cmd == "explain":
        plan = generate_plan(use_llm=use_llm)
        print(json.dumps(plan, indent=2))
    else:
        print(f"Unknown command: {cmd}")
        sys.exit(1)
