#!/usr/bin/env python3
"""Validate milestone 00 structure and architectural constraints without Unity."""

from __future__ import annotations

import json
import argparse
from collections import Counter, defaultdict
import re
import sys
import subprocess
from pathlib import Path


ROOT = Path(__file__).resolve().parent.parent
ASSETS = ROOT / "Assets"
PROJECT = ASSETS / "_Project"
EXPECTED_DEPENDENCIES = {
    "SocialStim.Rules": set(),
    "SocialStim.Application": {"SocialStim.Rules"},
    "SocialStim.Data": {"SocialStim.Rules"},
    "SocialStim.Presentation": {
        "SocialStim.Rules", "SocialStim.Application", "SocialStim.Data",
    },
    "SocialStim.Editor": {
        "SocialStim.Rules", "SocialStim.Application", "SocialStim.Data", "SocialStim.Presentation",
    },
    "SocialStim.Tests.EditMode": {
        "SocialStim.Rules", "SocialStim.Application", "SocialStim.Data",
    },
    "SocialStim.Tests.PlayMode": {
        "SocialStim.Rules", "SocialStim.Application", "SocialStim.Data",
        "SocialStim.Presentation",
    },
}
PACKAGE_ASSEMBLIES = {"UnityEngine.UI", "Unity.TextMeshPro", "Unity.InputSystem"}
EXPECTED_PACKAGES = {
    "com.unity.inputsystem": "1.17.0",
    "com.unity.test-framework": "1.6.0",
    "com.unity.ugui": "2.0.0",
    "com.unity.modules.imgui": "1.0.0",
    "com.unity.modules.jsonserialize": "1.0.0",
    "com.unity.modules.ui": "1.0.0",
    "com.unity.modules.uielements": "1.0.0",
}


def dialogue_coverage(parsed: dict[Path, dict]) -> tuple[list[str], str]:
    """Extend the existing source validator for the current schema's voice pools."""
    failures: list[str] = []
    source = (PROJECT / "Rules/Cast/Personality.cs").read_text()
    traits = [name for name in re.findall(r"\b(\w+)\s*=\s*\d+", source) if name != "None"]
    intent_source = (PROJECT / "Rules/Dialogue/SpeechIntent.cs").read_text()
    intents = [name.strip() for name in re.search(r"enum SpeechIntent\s*\{([^}]+)", intent_source).group(1).split(",")]
    frequent = {"Accuse", "Defend", "ClaimRole", "Agree", "Disagree", "ReactToDeath", "VoteJustification"}
    files = sorted(path for path in parsed if path.parent == PROJECT / "Data/Dialogue" and path.name.endswith("Dialogue.json"))
    rows = [row for path in files for row in parsed[path].get("lines", []) if isinstance(row, dict)]
    seen_ids: set[str] = set()
    seen_text: dict[str, str] = {}
    shingles: list[set[str]] = []
    owners: dict[str, list[int]] = defaultdict(list)
    counts: Counter = Counter()
    word_counts: list[int] = []
    for row in rows:
        identity = row.get("id", "missing-id")
        if identity in seen_ids:
            failures.append(f"Duplicate dialogue ID across files: {identity}")
        seen_ids.add(identity)
        trait = row.get("requiredTrait", "None")
        intent = row.get("intent")
        if trait != "None" and trait not in traits:
            failures.append(f"{identity}: choose exactly one registered trait")
        if intent not in intents:
            failures.append(f"{identity}: unknown action {intent}")
        counts[trait, intent] += 1
        text = row.get("text", "")
        plain = re.sub(r"<[^>]*>", "", text)
        tokens = re.findall(r"[a-z0-9_{}']+", plain.lower())
        word_counts.append(len(text.split()))
        normalized = " ".join(tokens)
        if normalized in seen_text:
            failures.append(f"Duplicate dialogue text: {identity} / {seen_text[normalized]}")
        seen_text[normalized] = identity
        pairs = {" ".join(tokens[i:i + 2]) for i in range(len(tokens) - 1)}
        candidates = {index for pair in pairs for index in owners[pair]}
        for index in candidates:
            other = shingles[index]
            union = len(pairs | other)
            if min(len(pairs), len(other)) >= 3 and union and len(pairs & other) / union >= 0.8:
                failures.append(f"Near-duplicate dialogue: {identity} / {rows[index].get('id')}")
        index = len(shingles)
        shingles.append(pairs)
        for pair in pairs:
            owners[pair].append(index)
        if len(text.split()) > (14 if trait == "None" else 40):
            failures.append(f"{identity}: authored text exceeds its word limit")
        if trait == "None" and any(mark in text for mark in ("<", ">", "!")):
            failures.append(f"{identity}: player text must be neutral and free of markup/exclamations")
        residue = re.sub(r"\{(?:speaker|target|role|result|day)\}", "", text)
        if "{" in residue or "}" in residue:
            failures.append(f"{identity}: unknown or malformed placeholder")
        for condition in row.get("conditions", []):
            if condition.get("kind", "").lower() == "speakertrait" and condition.get("value") != trait:
                failures.append(f"{identity}: trait condition conflicts with its single voice pool")
    for trait in ["None", *traits]:
        for intent in intents:
            pool = [row for row in rows if row.get("requiredTrait", "None") == trait and row.get("intent") == intent]
            minimum = 1 if trait == "None" else 8 if intent in frequent else 3
            if len(pool) < minimum:
                failures.append(f"Dialogue coverage {trait}/{intent}: {len(pool)} < {minimum}")
            if not any(not row.get("speakerRole") and not row.get("conditions") for row in pool):
                failures.append(f"Missing same-voice fallback: {trait}/{intent}")
        if trait != "None" and sum(counts[trait, intent] for intent in intents) < 100:
            failures.append(f"Trait {trait} has fewer than 100 lines")
    catalog = PROJECT / "ScriptableObjects/Catalogs/StarterCatalog.asset"
    names = re.findall(r"^  - (.+)$", catalog.read_text().split("  _names:\n", 1)[1].split("  _reasoning:", 1)[0], re.MULTILINE)
    if len(names) < 40 or len(names) != len(set(names)) or any(len(name) > 16 or not re.fullmatch(r"[A-Za-z ]+", name) for name in names):
        failures.append("NPC name pool needs 40+ distinct names of at most 16 ASCII letters/spaces")
    output = ["# Dialogue coverage", "", "Generated by `python3 Tools/validate_scaffold.py --write-dialogue-coverage`.", "",
              "Source: existing schema 2 JSON files in `Assets/_Project/Data/Dialogue`.", "",
              "| Trait | " + " | ".join(intents) + " | Total | 100 minimum |",
              "| --- | " + " | ".join(["---:"] * len(intents)) + " | ---: | --- |"]
    for trait in traits:
        total = sum(counts[trait, intent] for intent in intents)
        output.append("| " + trait + " | " + " | ".join(str(counts[trait, intent]) for intent in intents)
                      + f" | {total} | {'PASS' if total >= 100 else 'FAIL'} |")
    players = sum(counts["None", intent] for intent in intents)
    output += ["", f"NPC lines: **{len(rows) - players}**. Neutral player templates: **{players}**. Total: **{len(rows)}**.",
               f"Names: **{len(names)}**. Distinct normalized text values: **{len(seen_text)}**.", "",
               "Every frequent NPC action requires at least eight lines; other registered actions require three.",
               "Every voice/action must retain an unrestricted fallback in its own pool.",
               "Player source templates have a 14-word maximum; NPC source lines have a 40-word maximum.",
               "Near-duplicate lint rejects normalized equality and word-bigram Jaccard similarity >= 0.8",
               "for lines with at least three bigrams. This lexical check does not establish semantic originality.",
               "", "ReportAction retains the existing opaque `{result}` substitution; a recorded report may",
               "exceed the template word limit after expansion. No new reason/truthfulness/stat/cooldown",
               "fields or gameplay actions are implied by this coverage report.", ""]
    return failures, "\n".join(output)


def validate() -> list[str]:
    """Return actionable failures without modifying project files."""
    errors: list[str] = []

    def require(condition: bool, message: str) -> None:
        if not condition:
            errors.append(message)

    # Inspect versioned inputs, not ignored Unity/.NET output from an earlier check.
    # Include new source files so local validation also works before git add.
    result = subprocess.run(
        ["git", "ls-files", "--cached", "--others", "--exclude-standard", "-z"],
        cwd=ROOT, check=True, capture_output=True,
    )
    # Deduplicate names before making Path objects: WindowsPath equality folds
    # case and would otherwise conceal exactly the collisions being checked.
    names = sorted(set(result.stdout.decode("utf-8").split("\0")))
    files = [ROOT / name for name in names if name and (ROOT / name).is_file()]
    folded: dict[str, str] = {}
    for path in files:
        relative = path.relative_to(ROOT).as_posix()
        key = relative.casefold()
        require(key not in folded, f"Case-colliding paths: {folded.get(key)} and {relative}")
        folded[key] = relative
    imported = [
        path for path in ASSETS.rglob("*")
        if not any(part.startswith(".") for part in path.relative_to(ASSETS).parts)
        and path.suffix != ".meta"
    ]
    guids: dict[str, Path] = {}
    asset_guids: dict[Path, str] = {}
    for meta in sorted(ASSETS.rglob("*.meta")):
        target = Path(str(meta)[:-5])
        require(target.exists(), f"Orphan metadata: {meta.relative_to(ROOT)}")
        source = meta.read_text(encoding="utf-8")
        match = re.search(r"^guid: ([0-9a-f]{32})$", source, re.MULTILINE)
        require(match is not None, f"Invalid GUID: {meta.relative_to(ROOT)}")
        require(source.startswith("fileFormatVersion: 2\n"), f"Bad metadata header: {meta}")
        if match:
            value = match.group(1)
            require(value not in guids, f"Duplicate GUID: {meta.relative_to(ROOT)}")
            guids[value] = target
            asset_guids[target] = value
        require(
            ("folderAsset: yes" in source) == target.is_dir(),
            f"Folder metadata mismatch: {meta.relative_to(ROOT)}",
        )
        if target.suffix == ".asmdef":
            require("AssemblyDefinitionImporter:" in source, f"Wrong importer: {meta}")
    for path in imported:
        require(Path(str(path) + ".meta").is_file(), f"Missing metadata: {path.relative_to(ROOT)}")

    # Unity's built-in references use reserved zero-prefixed GUIDs. Every other
    # serialized project-owned reference must resolve to committed metadata.
    # Vendor assets imported outside _Project can refer to package-owned scripts.
    for path in files:
        if not path.is_relative_to(PROJECT) or path.suffix not in {".asset", ".unity", ".prefab"}:
            continue
        for guid in re.findall(r"\bguid: ([0-9a-f]{32})\b", path.read_text(encoding="utf-8")):
            require(guid in guids or guid.startswith("0000000000000000"),
                    f"Unresolved asset GUID {guid}: {path.relative_to(ROOT)}")

    parsed: dict[Path, dict] = {}
    for path in files:
        if path.suffix in {".json", ".asmdef"}:
            try:
                parsed[path] = json.loads(path.read_text(encoding="utf-8"))
            except (ValueError, UnicodeError) as error:
                errors.append(f"Invalid JSON in {path.relative_to(ROOT)}: {error}")

    assemblies: dict[str, dict] = {}
    assembly_names: dict[str, str] = {}
    for path in PROJECT.rglob("*.asmdef"):
        definition = parsed.get(path)
        if not isinstance(definition, dict):
            continue
        name = definition.get("name")
        require(isinstance(name, str), f"Missing assembly name: {path}")
        if not isinstance(name, str):
            continue
        require(name not in assemblies, f"Duplicate assembly: {name}")
        require(path.stem == name, f"Assembly filename mismatch: {path}")
        require(definition.get("rootNamespace") == name, f"Namespace mismatch: {name}")
        require(definition.get("autoReferenced") is False, f"Implicit assembly access: {name}")
        require(definition.get("allowUnsafeCode") is False, f"Unsafe code enabled: {name}")
        require(not definition.get("overrideReferences"), f"Unexpected overrideReferences: {name}")
        require(not definition.get("excludePlatforms"), f"Unexpected exclusions: {name}")
        require(not definition.get("precompiledReferences"), f"Unexpected binary references: {name}")
        assemblies[name] = definition
        if path in asset_guids:
            assembly_names[asset_guids[path]] = name

    require(set(assemblies) == set(EXPECTED_DEPENDENCIES), "Assembly set differs from milestone 00 policy")
    graph: dict[str, set[str]] = {}
    for name, definition in assemblies.items():
        dependencies: set[str] = set()
        external: set[str] = set()
        references = definition.get("references", [])
        require(isinstance(references, list), f"References must be a list: {name}")
        if not isinstance(references, list):
            continue
        require(len(references) == len(set(references)), f"Duplicate references: {name}")
        for reference in references:
            if reference.startswith("GUID:"):
                resolved = assembly_names.get(reference[5:])
                require(resolved is not None, f"Unresolved assembly GUID in {name}: {reference}")
                if resolved:
                    dependencies.add(resolved)
            elif reference.startswith("SocialStim."):
                errors.append(f"Use a GUID project reference in {name}: {reference}")
            else:
                external.add(reference)
        require(dependencies == EXPECTED_DEPENDENCIES.get(name), f"Forbidden/missing project dependency: {name}")
        expected_external = PACKAGE_ASSEMBLIES if name == "SocialStim.Presentation" else (
            {"UnityEngine.UI", "Unity.TextMeshPro"} if name == "SocialStim.Tests.PlayMode" else set()
        )
        require(external == expected_external, f"Unexpected/missing package assembly reference: {name}")
        engine_free = name in {"SocialStim.Rules", "SocialStim.Application"}
        require(definition.get("noEngineReferences") is engine_free, f"Engine boundary changed: {name}")
        editor_only = name in {"SocialStim.Editor", "SocialStim.Tests.EditMode"}
        require(definition.get("includePlatforms") == (["Editor"] if editor_only else []), f"Platform boundary changed: {name}")
        is_test = name.startswith("SocialStim.Tests.")
        require(definition.get("defineConstraints") == (["UNITY_INCLUDE_TESTS"] if is_test else []), f"Test constraint changed: {name}")
        require(definition.get("optionalUnityReferences", []) == (["TestAssemblies"] if is_test else []), f"Test assembly marker changed: {name}")
        graph[name] = dependencies

    visited: set[str] = set()
    active: set[str] = set()

    def visit(name: str) -> None:
        if name in active:
            errors.append(f"Assembly cycle at {name}")
            return
        if name in visited:
            return
        active.add(name)
        for dependency in graph.get(name, set()):
            visit(dependency)
        active.remove(name)
        visited.add(name)

    for name in graph:
        visit(name)

    manifest = parsed.get(ROOT / "Packages/manifest.json", {})
    require(manifest.get("dependencies") == EXPECTED_PACKAGES, "Direct package pins changed; review milestone policy")
    version = ROOT / "ProjectSettings/ProjectVersion.txt"
    require(version.is_file() and "m_EditorVersion: 6000.3.26f1\n" in version.read_text(), "Editor version must be 6000.3.26f1")
    settings = ROOT / "ProjectSettings/EditorSettings.asset"
    require(settings.is_file() and re.search(r"^  m_SerializationMode: 2$", settings.read_text(), re.MULTILINE) is not None, "Force Text is not configured")
    control = ROOT / "ProjectSettings/VersionControlSettings.asset"
    require(control.is_file() and "m_Mode: Visible Meta Files" in control.read_text(), "Visible metadata is not configured")

    prohibited_roots = {
        "library", "temp", "logs", "usersettings", "obj", "build", "builds",
        "testresults", "artifacts", "memorycaptures", "recordings", "coverage",
    }
    for path in files:
        require(path.relative_to(ROOT).parts[0].lower() not in prohibited_roots,
                f"Versioned generated project state: {path.relative_to(ROOT)}")
    for path in files:
        relative = path.relative_to(ROOT)
        require(not any(part in {".idea", ".vs", ".vscode"} for part in relative.parts), f"IDE state present: {relative}")
        require(path.suffix.lower() not in {".sln", ".slnx", ".user", ".pem", ".key", ".pfx", ".p12", ".keystore", ".jks"}
            and (path.suffix.lower() != ".csproj" or relative.as_posix() == "Tools/HeadlessTests/SocialStim.HeadlessTests.csproj"), f"Generated/credential file: {relative}")
        require(not (path.name == ".env" or path.name.startswith(".env.") and path.name != ".env.example"), f"Environment credentials file: {relative}")
        if path.suffix in {".cs", ".md", ".json", ".asmdef", ".meta", ".txt", ".asset", ".py", ".unity"} or path.name in {".editorconfig", ".gitattributes", ".gitignore"}:
            data = path.read_bytes()
            require(b"\r" not in data, f"Expected LF line endings: {relative}")
            require(not data or data.endswith(b"\n"), f"Missing final newline: {relative}")
            try:
                lines = data.decode("utf-8").splitlines()
            except UnicodeError:
                errors.append(f"Expected UTF-8 text: {relative}")
                continue
            require(all(line == line.rstrip() for line in lines), f"Trailing whitespace: {relative}")
            require(not any(re.match(r"^(<{7,}|={7,}|>{7,})(?: |$)", line) for line in lines),
                    f"Unresolved merge conflict: {relative}")
        if path.suffix == ".cs":
            parent = path.parent
            while parent != ASSETS and not list(parent.glob("*.asmdef")):
                parent = parent.parent
                if parent == ROOT:
                    break
            require(parent != ASSETS and parent != ROOT, f"Script outside a project assembly: {relative}")

    dialogue_errors, coverage = dialogue_coverage(parsed)
    errors.extend(dialogue_errors)
    report = ROOT / "Docs/DIALOGUE_COVERAGE.md"
    require(report.is_file() and report.read_text(encoding="utf-8") == coverage,
            "Dialogue coverage is stale; run python3 Tools/validate_scaffold.py --write-dialogue-coverage")
    return errors


def main() -> int:
    """Print failures and return a process exit code suitable for local checks."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--write-dialogue-coverage", action="store_true", help="Refresh the dialogue coverage report before checking source")
    args = parser.parse_args()
    try:
        if args.write_dialogue_coverage:
            documents = {path: json.loads(path.read_text(encoding="utf-8"))
                         for path in (PROJECT / "Data/Dialogue").glob("*Dialogue.json")}
            _, report = dialogue_coverage(documents)
            (ROOT / "Docs/DIALOGUE_COVERAGE.md").write_text(report, encoding="utf-8")
        failures = validate()
    except (OSError, TypeError, AttributeError, subprocess.CalledProcessError) as error:
        print(f"FAIL: Unable to validate scaffold: {error}", file=sys.stderr)
        return 1
    if failures:
        for failure in failures:
            print(f"FAIL: {failure}", file=sys.stderr)
        return 1
    print("PASS: 7 assembly definitions; metadata, dependency boundaries, JSON, pins, and hygiene.")
    print("Unity import, package resolution, adapter compilation, and editor/player tests: NOT RUN by this tool.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
