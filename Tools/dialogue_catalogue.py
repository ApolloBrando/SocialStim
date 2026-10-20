#!/usr/bin/env python3
"""Generate the complete browsable dialogue/copy reference from the editable assets."""

from __future__ import annotations

import argparse
import html
import json
import re
from collections import defaultdict
from pathlib import Path


ROOT = Path(__file__).resolve().parent.parent
SPEECH = ROOT / "Assets/_Project/Data/Dialogue/StarterDialogue.json"
COPY = ROOT / "Assets/_Project/Data/Dialogue/Resources/SocialStimText.json"
OUTPUT = ROOT / "Docs/DIALOGUE_CATALOGUE.md"
TOKEN = re.compile(r"\{([A-Za-z][A-Za-z0-9]*)\}")


def cell(value: str) -> str:
    """Keep authoring markup visible rather than interpreting it as Markdown/HTML."""
    return html.escape(value, quote=False).replace("|", "&#124;").replace("\n", "<br>")


def load(path: Path, field: str) -> list[dict]:
    """Reject duplicate JSON properties and IDs before documenting an asset."""
    def unique(pairs: list[tuple]) -> dict:
        result = {}
        for key, value in pairs:
            if key in result:
                raise ValueError(f"{path.name}: duplicate JSON property {key}")
            result[key] = value
        return result

    document = json.loads(path.read_text(encoding="utf-8"), object_pairs_hook=unique)
    rows = document[field]
    if len({row["id"] for row in rows}) != len(rows):
        raise ValueError(f"{path.name}: duplicate content ID")
    return rows


def render() -> str:
    """Render every source line and template; source files remain authoritative."""
    speech = load(SPEECH, "lines")
    copy = load(COPY, "entries")
    copy_ids = {row["id"] for row in copy}
    for path in (ROOT / "Assets/_Project").rglob("*.cs"):
        if "Tests" in path.parts:
            continue
        for key in re.findall(r'\b(?:_text|_copy|text)\.Render\("([^"]+)"\s*(?=,|\))', path.read_text()):
            if key not in copy_ids:
                raise ValueError(f"{path.relative_to(ROOT)}: missing text ID {key}")
    contract = ROOT / "Assets/_Project/Rules/Dialogue/TextCatalogContract.cs"
    required_block = contract.read_text().split("private static readonly IReadOnlyDictionary", 1)[1].split("};", 1)[0]
    by_id = {row["id"]: row["text"] for row in copy}
    for line in required_block.splitlines():
        if not line.strip().startswith('{ "'):
            continue
        values = re.findall(r'"([^"]+)"', line)
        key, *allowed = values
        if key not in copy_ids:
            raise ValueError(f"Missing required text ID: {key}")
        unavailable = set(TOKEN.findall(by_id[key])) - set(allowed)
        if unavailable:
            raise ValueError(f"{key}: unavailable tokens {sorted(unavailable)}")
    for row in copy:
        if not row["text"].strip() or "{" in TOKEN.sub("", row["text"]) or "}" in TOKEN.sub("", row["text"]):
            raise ValueError(f"{row['id']}: empty copy or malformed token")

    lines = [
        "# Complete dialogue and game-text catalogue", "",
        "Generated from the editable JSON files. Edit the source, then run",
        "`python3 Tools/dialogue_catalogue.py`. CI checks this document stays synchronized.", "",
        f"Current content: **{len(speech)} spoken lines** and **{len(copy)} narration/interface templates**.",
        "Templates are not additional conversational variants and do not count toward the 500 player-line target.", "",
        "| Edit | Source |", "| --- | --- |",
        "| Spoken lines shared by players and NPCs | [StarterDialogue.json](../Assets/_Project/Data/Dialogue/StarterDialogue.json) |",
        "| Narration, night results, choice labels, menus and help | [SocialStimText.json](../Assets/_Project/Data/Dialogue/Resources/SocialStimText.json) |",
        "| Editing instructions | [DIALOGUE_GUIDE.md](DIALOGUE_GUIDE.md) |", "",
        "## Spoken lines", "",
        "Eligibility still depends on the current role, phase and command. A line listed here is not necessarily available now.", "",
    ]
    groups = defaultdict(list)
    for row in speech:
        groups[row["intent"]].append(row)
    for intent, rows in sorted(groups.items()):
        lines += [f"### {intent} ({len(rows)})", "", "| Stable ID | Exact authored text | Other fields |", "| --- | --- | --- |"]
        for row in sorted(rows, key=lambda r: r["id"]):
            other = {key: value for key, value in row.items() if key not in {"id", "intent", "text"}}
            lines.append(f"| `{row['id']}` | {cell(row['text'])} | {cell(json.dumps(other, ensure_ascii=False, sort_keys=True))} |")
        lines.append("")
    lines += ["## Narration, result and interface templates", "", "Named tokens are substituted from authorized game state. They can be reordered in the source.", ""]
    groups.clear()
    for row in copy:
        groups[row["id"].split(".", 1)[0]].append(row)
    for group, rows in sorted(groups.items()):
        lines += [f"### {group} ({len(rows)})", "", "| Stable ID | Exact authored template | Tokens |", "| --- | --- | --- |"]
        for row in sorted(rows, key=lambda r: r["id"]):
            tokens = ", ".join(sorted(set(TOKEN.findall(row["text"])))) or "—"
            lines.append(f"| `{row['id']}` | {cell(row['text'])} | {tokens} |")
        lines.append("")
    return "\n".join(lines)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check", action="store_true", help="Fail if the committed reference differs from the editable assets.")
    args = parser.parse_args()
    try:
        expected = render()
        if args.check:
            if not OUTPUT.exists() or OUTPUT.read_text(encoding="utf-8") != expected:
                raise ValueError("Dialogue catalogue is stale; run python3 Tools/dialogue_catalogue.py")
            print("PASS: all dialogue/copy entries, required bindings and catalogue freshness.")
        else:
            OUTPUT.write_text(expected, encoding="utf-8")
            print(f"Wrote {OUTPUT.relative_to(ROOT)}")
    except (OSError, ValueError, KeyError, TypeError) as error:
        print(f"FAIL: {error}")
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
