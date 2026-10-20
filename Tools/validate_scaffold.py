#!/usr/bin/env python3
"""Validate milestone 00 structure and architectural constraints without Unity."""

from __future__ import annotations

import json
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
    files = sorted({ROOT / name for name in result.stdout.decode("utf-8").split("\0")
                    if name and (ROOT / name).is_file()})
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
    # serialized project reference must resolve to committed metadata.
    for path in files:
        if path.suffix not in {".asset", ".unity", ".prefab"}:
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

    return errors


def main() -> int:
    """Print failures and return a process exit code suitable for local checks."""
    try:
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
