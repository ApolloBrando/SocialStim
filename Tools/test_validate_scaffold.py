"""Exercise validation against real repository copies and merge/import failures."""

import shutil
import subprocess
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

import validate_scaffold as validator


class ScaffoldValidationTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.temporary = tempfile.TemporaryDirectory(dir=validator.ROOT.parent)
        cls.root = Path(cls.temporary.name)
        result = subprocess.run(
            ["git", "ls-files", "--cached", "--others", "--exclude-standard", "-z"],
            cwd=validator.ROOT, check=True, capture_output=True,
        )
        for name in set(result.stdout.decode().split("\0")):
            source = validator.ROOT / name
            if not name or not source.is_file():
                continue
            destination = cls.root / name
            destination.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(source, destination)
        subprocess.run(["git", "init", "--quiet"], cwd=cls.root, check=True)

    @classmethod
    def tearDownClass(cls):
        cls.temporary.cleanup()

    def validate(self):
        with patch.multiple(validator, ROOT=self.root, ASSETS=self.root / "Assets",
                            PROJECT=self.root / "Assets/_Project"):
            return validator.validate()

    def test_clean_source_and_ignored_build_output_both_pass(self):
        self.assertEqual([], self.validate())
        output = self.root / "Tools/HeadlessTests/obj/generated.cs"
        output.parent.mkdir(parents=True, exist_ok=True)
        results = self.root / "TestResults/result.trx"
        results.parent.mkdir(parents=True, exist_ok=True)
        try:
            output.write_text("not source code\n")
            results.write_text("not versioned\n")
            self.assertEqual([], self.validate())
        finally:
            shutil.rmtree(output.parent)
            shutil.rmtree(results.parent)

    def test_case_collision_and_conflict_markers_fail(self):
        first = self.root / "Docs/MergeGuard.md"
        second = self.root / "Docs/mergeguard.md"
        try:
            first.write_text("<<<<<<< ours\nold\n=======\nnew\n>>>>>>> theirs\n")
            second.write_text("Other document.\n")
            failures = self.validate()
            self.assertTrue(any("Case-colliding paths" in error for error in failures))
            self.assertTrue(any("Unresolved merge conflict" in error for error in failures))
        finally:
            first.unlink(missing_ok=True)
            second.unlink(missing_ok=True)

    def test_missing_serialized_reference_fails(self):
        scene = self.root / "Assets/_Project/Scenes/Game.unity"
        original = scene.read_text()
        try:
            scene.write_text(original.replace("65084cb8baf15b47b22b10f8a6311a78", "1234567890abcdef1234567890abcdef"))
            self.assertTrue(any("Unresolved asset GUID" in error for error in self.validate()))
        finally:
            scene.write_text(original)

    def test_missing_metadata_fails(self):
        path = self.root / "Assets/_Project/Scenes/Game.unity.meta"
        original = path.read_bytes()
        try:
            path.unlink()
            self.assertTrue(any("Missing metadata" in error for error in self.validate()))
        finally:
            path.write_bytes(original)


if __name__ == "__main__":
    unittest.main()
