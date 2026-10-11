# Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.
"""Regression tests for the fast CI validators; no Unity installation required."""

import contextlib
import io
import json
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import check_meta_files as meta
import check_package_json as package


class MetaTests(unittest.TestCase):
    def test_assets_ignored_subtrees_and_orphans(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary) / "Unity"
            asset_root = root / "com.amd.schola"
            asset_root.mkdir(parents=True)
            (asset_root / "Runtime").mkdir()
            (asset_root / "Runtime.meta").touch()
            asset = asset_root / "Runtime" / "Agent.cs"
            asset.touch()
            asset.with_name("Agent.cs.meta").touch()
            for ignored in (".hidden", "Samples~", "Documentation~"):
                (asset_root / ignored).mkdir()
                (asset_root / ignored / "Untracked.cs").touch()
            (asset_root / ".gitkeep").touch()
            (asset_root / ".hidden.meta").touch()
            self.assertEqual(meta.validate(root), [])
            asset.with_name("Agent.cs.meta").unlink()
            (asset_root / "Orphan.cs.meta").touch()
            (asset_root / "MissingFolder").mkdir()
            problems = meta.validate(root)
            self.assertEqual(len(problems), 3)
            self.assertTrue(any("Agent.cs: missing .meta" in p for p in problems))
            self.assertTrue(any("MissingFolder: missing .meta" in p for p in problems))
            self.assertTrue(
                any("Orphan.cs.meta: orphaned .meta" in p for p in problems)
            )
            with patch.object(
                meta, "REPO_ROOT", Path(temporary)
            ), contextlib.redirect_stdout(io.StringIO()):
                self.assertEqual(meta.main(), 1)

    def test_missing_unity_directory_fails(self):
        with tempfile.TemporaryDirectory() as temporary:
            self.assertEqual(len(meta.validate(Path(temporary) / "Unity")), 1)


class PackageTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.path = Path(self.temporary.name) / "package.json"
        self.manifest = {
            "name": "com.amd.schola",
            "version": "1.2.3",
            "unity": "6000.3",
            "displayName": "Schola",
            "description": "Unity package",
        }

    def write(self):
        self.path.write_text(json.dumps(self.manifest), encoding="utf-8")

    def test_valid_manifest_and_prerelease_dependencies(self):
        self.manifest["version"] = "1.2.3-rc.1"
        self.manifest["dependencies"] = {"com.example.package": "0.1.0-preview.2"}
        self.write()
        self.assertEqual(package.validate(self.path), [])
        with patch.object(package, "MANIFEST", self.path), contextlib.redirect_stdout(
            io.StringIO()
        ):
            self.assertEqual(package.main(), 0)

    def test_all_field_errors_are_reported(self):
        self.manifest.update(
            name="wrong",
            version="01.2.3",
            unity=6000,
            displayName=" ",
            description=None,
            dependencies={"bad": 42},
        )
        self.write()
        self.assertEqual(len(package.validate(self.path)), 6)
        with patch.object(package, "MANIFEST", self.path), contextlib.redirect_stdout(
            io.StringIO()
        ):
            self.assertEqual(package.main(), 1)

    def test_invalid_json_missing_file_and_non_object(self):
        self.assertTrue(package.validate(self.path))
        self.path.write_text("{", encoding="utf-8")
        self.assertTrue(package.validate(self.path))
        self.path.write_text("[]", encoding="utf-8")
        self.assertTrue(package.validate(self.path))

    def test_invalid_dependency_container(self):
        for dependencies in (None, [], "1.2.3"):
            with self.subTest(dependencies=dependencies):
                self.manifest["dependencies"] = dependencies
                self.write()
                self.assertEqual(len(package.validate(self.path)), 1)

    def test_semantic_versions(self):
        for version in ("0.0.0", "1.2.3", "1.2.3-alpha.1", "1.2.3-0", "1.2.3-1a"):
            self.assertTrue(package.version_string(version), version)
        for version in (
            "1.2",
            "01.2.3",
            "1.2.3-01",
            "1.2.3-",
            "v1.2.3",
            "1.2.3+build",
            123,
        ):
            self.assertFalse(package.version_string(version), version)


if __name__ == "__main__":
    unittest.main()
