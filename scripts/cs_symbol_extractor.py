"""
cs_symbol_extractor.py — Phase 7c: TreeSitter-based C# AST Symbol Extraction

Parses changed C# files using tree-sitter to extract only relevant symbols
(class names, method signatures, record types) into a compact JSON bundle.
This compact representation is injected into context.json instead of full file contents,
saving 60-80% of the tokens that would otherwise be spent on raw file reads.

Usage:
    python scripts/cs_symbol_extractor.py extract              # Extract symbols for feature_path
    python scripts/cs_symbol_extractor.py diff                 # Extract symbols only for git-changed files
    python scripts/cs_symbol_extractor.py show                 # Print current symbol bundle

Config:
    Reads context.json for feature_path to scope the extraction.
    Writes output to .agent_artifacts/symbol_bundle.json
"""
import os
import sys
import json
import subprocess
import datetime

PROJECT_ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ARTIFACTS_DIR = os.path.join(PROJECT_ROOT, ".agent_artifacts")
CONTEXT_FILE = os.path.join(ARTIFACTS_DIR, "context.json")
SYMBOL_BUNDLE = os.path.join(ARTIFACTS_DIR, "symbol_bundle.json")


def _load_context() -> dict:
    if not os.path.exists(CONTEXT_FILE):
        return {}
    with open(CONTEXT_FILE, encoding="utf-8") as f:
        return json.load(f)


def _get_changed_cs_files() -> list:
    """Return list of .cs files changed since last commit via git diff."""
    result = subprocess.run(
        ["git", "diff", "--name-only", "HEAD"],
        cwd=PROJECT_ROOT,
        capture_output=True,
        text=True,
    )
    files = [
        os.path.join(PROJECT_ROOT, f.strip())
        for f in result.stdout.splitlines()
        if f.strip().endswith(".cs")
    ]
    return [f for f in files if os.path.exists(f)]


def _extract_symbols_regex(filepath: str) -> dict:
    """
    Lightweight regex-based symbol extraction as TreeSitter C# grammar fallback.
    Extracts: namespace, class names, record names, public method signatures.
    """
    import re

    with open(filepath, encoding="utf-8", errors="replace") as f:
        content = f.read()

    namespace = re.search(r"namespace\s+([\w.]+)", content)
    classes = re.findall(r"(?:public|internal|sealed|abstract)\s+(?:class|record|interface)\s+(\w+)", content)
    methods = re.findall(
        r"(?:public|private|protected|internal|override)\s+(?:async\s+)?[\w<>?[\],\s]+\s+(\w+)\s*\([^)]*\)",
        content,
    )
    enums = re.findall(r"public\s+enum\s+(\w+)", content)
    implements = re.findall(r":\s*([\w<>]+(?:,\s*[\w<>]+)*)", content)

    return {
        "file": os.path.relpath(filepath, PROJECT_ROOT).replace("\\", "/"),
        "namespace": namespace.group(1) if namespace else None,
        "classes": list(set(classes)),
        "methods": list(set(methods))[:15],  # cap at 15 per file
        "enums": list(set(enums)),
        "implements": list(set(implements))[:5],
    }


def _try_treesitter_extract(filepath: str) -> dict:
    """
    Try to use tree-sitter for precise C# AST parsing.
    Falls back to regex if tree-sitter-c-sharp grammar not available.
    """
    try:
        from tree_sitter import Language, Parser
        import tree_sitter_c_sharp as tscsharp

        CS_LANGUAGE = Language(tscsharp.language())
        parser = Parser(CS_LANGUAGE)

        with open(filepath, "rb") as f:
            source = f.read()

        tree = parser.parse(source)
        root = tree.root_node

        def _find_nodes(node, node_type):
            results = []
            if node.type == node_type:
                results.append(node)
            for child in node.children:
                results.extend(_find_nodes(child, node_type))
            return results

        def _text(node):
            return source[node.start_byte:node.end_byte].decode("utf-8", errors="replace")

        classes = [_text(n) for n in _find_nodes(root, "class_declaration")]
        records = [_text(n)[:80] for n in _find_nodes(root, "record_declaration")]
        methods = [_text(n.child_by_field_name("name")) for n in _find_nodes(root, "method_declaration") if n.child_by_field_name("name")]
        namespaces = [_text(n.child_by_field_name("name")) for n in _find_nodes(root, "namespace_declaration") if n.child_by_field_name("name")]

        return {
            "file": os.path.relpath(filepath, PROJECT_ROOT).replace("\\", "/"),
            "parser": "tree-sitter",
            "namespace": namespaces[0] if namespaces else None,
            "classes": [c.split("{")[0].strip()[:80] for c in classes][:5],
            "records": [r.strip()[:80] for r in records][:5],
            "methods": methods[:15],
        }
    except (ImportError, Exception):
        # tree-sitter-c-sharp grammar not installed — fall back to regex
        result = _extract_symbols_regex(filepath)
        result["parser"] = "regex-fallback"
        return result


def extract_symbols(file_paths: list) -> dict:
    """Extract symbols from a list of .cs files and write to symbol_bundle.json."""
    ctx = _load_context()
    jira_key = ctx.get("jira_key", "UNKNOWN")

    symbols = []
    for fp in file_paths:
        try:
            sym = _try_treesitter_extract(fp)
            symbols.append(sym)
            print(f"[SYMBOLS] Extracted: {sym['file']} ({len(sym.get('methods', []))} methods, parser={sym.get('parser', 'unknown')})")
        except Exception as e:
            print(f"[SYMBOLS] Failed on {fp}: {e}", file=sys.stderr)

    bundle = {
        "jira_key": jira_key,
        "extracted_at": datetime.datetime.utcnow().isoformat() + "Z",
        "file_count": len(symbols),
        "symbols": symbols,
    }

    os.makedirs(ARTIFACTS_DIR, exist_ok=True)
    with open(SYMBOL_BUNDLE, "w", encoding="utf-8") as f:
        json.dump(bundle, f, indent=2)

    # Also inject compact summary into context.json for gates to use
    if os.path.exists(CONTEXT_FILE):
        with open(CONTEXT_FILE, encoding="utf-8") as f:
            ctx = json.load(f)
        ctx["symbol_bundle_path"] = SYMBOL_BUNDLE
        ctx["changed_symbols_summary"] = [
            f"{s['file']}: {', '.join(s.get('classes', []) + s.get('records', []) + s.get('methods', [])[:3])}"
            for s in symbols
        ]
        with open(CONTEXT_FILE, "w", encoding="utf-8") as f:
            json.dump(ctx, f, indent=2)

    print(f"[SYMBOLS] Bundle written to {SYMBOL_BUNDLE} ({len(symbols)} files)")
    return bundle


def extract_feature_symbols():
    """Extract symbols for all .cs files in the current task feature_path."""
    ctx = _load_context()
    feature_path = ctx.get("feature_path", "src/Buy2.Application/")
    abs_path = os.path.join(PROJECT_ROOT, feature_path.replace("/", os.sep))

    cs_files = []
    if os.path.exists(abs_path):
        for root, _, files in os.walk(abs_path):
            for f in files:
                if f.endswith(".cs"):
                    cs_files.append(os.path.join(root, f))

    if not cs_files:
        print(f"[SYMBOLS] No .cs files found under {feature_path}")
        return {}

    return extract_symbols(cs_files)


def extract_diff_symbols():
    """Extract symbols only for git-changed .cs files (most token-efficient)."""
    changed = _get_changed_cs_files()
    if not changed:
        print("[SYMBOLS] No changed .cs files detected in git diff HEAD.")
        return {}
    return extract_symbols(changed)


if __name__ == "__main__":
    cmd = sys.argv[1] if len(sys.argv) > 1 else "diff"
    if cmd == "extract":
        extract_feature_symbols()
    elif cmd == "diff":
        extract_diff_symbols()
    elif cmd == "show":
        if os.path.exists(SYMBOL_BUNDLE):
            with open(SYMBOL_BUNDLE, encoding="utf-8") as f:
                print(f.read())
        else:
            print("[SYMBOLS] No symbol bundle found. Run extract or diff first.")
    else:
        print(f"Unknown command: {cmd}")
        sys.exit(1)
