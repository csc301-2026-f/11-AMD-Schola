"""Regression checks for missing/stale files and read-only codegen verification."""

from contextlib import redirect_stdout
import importlib.util
import io
import os
from pathlib import Path
import subprocess
import tempfile
import unittest
from unittest.mock import patch

SPEC = importlib.util.spec_from_file_location(
    "generate_csharp", Path(__file__).resolve().parents[1] / "generate_csharp.py"
)
codegen = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(codegen)


class CodegenTests(unittest.TestCase):
    def test_generation_rejects_tampered_tools_before_running_them(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            (root / "Test.proto").write_text('syntax = "proto3";')
            with patch.object(
                codegen.urllib.request, "urlopen", return_value=io.BytesIO(b"tampered")
            ), patch.object(codegen.subprocess, "run") as run:
                with self.assertRaisesRegex(RuntimeError, "checksum mismatch"):
                    codegen.generate(root, root / "output")
                run.assert_not_called()
            self.assertFalse((root / "output").exists())

    def test_empty_proto_directory_fails_without_downloading_tools(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            with patch.object(codegen.urllib.request, "urlopen") as download:
                with self.assertRaisesRegex(RuntimeError, "No .proto files"):
                    codegen.generate(root, root / "output")
                download.assert_not_called()

    def test_failed_generation_preserves_existing_baseline(self):
        with tempfile.TemporaryDirectory() as temporary:
            baseline = Path(temporary)
            source = baseline / "Message.cs"
            source.write_text("original\n")
            with patch.object(codegen, "GENERATED", baseline), patch.object(
                codegen,
                "generate",
                side_effect=subprocess.CalledProcessError(1, "protoc"),
            ), patch("sys.argv", ["generate_csharp.py"]):
                with self.assertRaises(subprocess.CalledProcessError):
                    codegen.main()
            self.assertEqual(source.read_text(), "original\n")

    def test_check_reports_drift_without_overwriting_existing_baseline(self):
        with tempfile.TemporaryDirectory() as temporary:
            baseline = Path(temporary)
            source = baseline / "Message.cs"
            source.write_text("original\n")

            def generate(_proto, output):
                (output / "Message.cs").write_text("updated\n")

            log = io.StringIO()
            with patch.object(codegen, "generate", side_effect=generate), patch(
                "sys.argv", ["generate_csharp.py", "--check", "--output", str(baseline)]
            ), redirect_stdout(log):
                self.assertEqual(codegen.main(), 1)
            self.assertEqual(source.read_text(), "original\n")
            self.assertIn(codegen.REGENERATE, log.getvalue())

    def test_workflow_detects_baseline_even_after_complete_deletion(self):
        # Exercise the actual workflow shell instead of duplicating its decision.
        workflow = (
            codegen.REPO_ROOT / ".github/workflows/codegen-check.yml"
        ).read_text()
        block = workflow.split("        run: |\n", 1)[1].split("      - name:", 1)[0]
        shell = "\n".join(line[10:] for line in block.splitlines())
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)

            def git(*args):
                subprocess.run(
                    [
                        "git",
                        "-c",
                        "user.name=Test",
                        "-c",
                        "user.email=test@example.com",
                        *args,
                    ],
                    cwd=root,
                    check=True,
                    capture_output=True,
                )

            def detect():
                output = root / "output"
                output.write_text("")
                subprocess.run(
                    ["bash", "-e", "-o", "pipefail", "-c", shell],
                    cwd=root,
                    check=True,
                    env={
                        **os.environ,
                        "GITHUB_OUTPUT": str(output),
                        "GITHUB_STEP_SUMMARY": str(root / "summary"),
                    },
                    capture_output=True,
                )
                return output.read_text().strip()

            git("init")
            git("commit", "--allow-empty", "-m", "Initial commit")
            self.assertEqual(detect(), "present=false")
            baseline = root / "Unity/com.amd.schola/Training/Generated"
            baseline.mkdir(parents=True)
            (baseline / "Message.cs").write_text("generated\n")
            git("add", ".")
            git("commit", "-m", "Add baseline")
            self.assertEqual(detect(), "present=true")
            git("rm", "Unity/com.amd.schola/Training/Generated/Message.cs")
            git("commit", "-m", "Delete baseline")
            self.assertEqual(detect(), "present=true")

    def test_comparison_detects_missing_stale_and_edited_sources(self):
        with tempfile.TemporaryDirectory() as temporary:
            expected = Path(temporary) / "expected"
            actual = Path(temporary) / "actual"
            expected.mkdir()
            actual.mkdir()
            (expected / "Missing.cs").write_text("generated\n")
            (actual / "Stale.cs").write_text("old\n")
            (expected / "Changed.cs").write_text("new\n")
            (actual / "Changed.cs").write_text("old\n")
            log = io.StringIO()
            with redirect_stdout(log):
                self.assertFalse(codegen.compare(expected, actual))
            self.assertIn("Missing generated file: Missing.cs", log.getvalue())
            self.assertIn("Stale generated file: Stale.cs", log.getvalue())
            self.assertIn("-old\n+new", log.getvalue())

    def test_comparison_ignores_unity_metadata(self):
        with tempfile.TemporaryDirectory() as temporary:
            expected = Path(temporary) / "expected"
            actual = Path(temporary) / "actual"
            expected.mkdir()
            actual.mkdir()
            for directory in (expected, actual):
                (directory / "Message.cs").write_text("same\n")
            (actual / "Message.cs.meta").write_text("unity metadata\n")
            self.assertTrue(codegen.compare(expected, actual))

    def test_check_fails_without_creating_missing_baseline(self):
        with tempfile.TemporaryDirectory() as temporary:
            baseline = Path(temporary) / "Generated"

            def generate(_proto, output):
                (output / "Message.cs").write_text("generated\n")

            with patch.object(codegen, "GENERATED", baseline), patch.object(
                codegen, "generate", side_effect=generate
            ), patch("sys.argv", ["generate_csharp.py", "--check"]), redirect_stdout(
                io.StringIO()
            ):
                self.assertEqual(codegen.main(), 1)
            self.assertFalse(baseline.exists())

    def test_regeneration_removes_stale_sources_and_preserves_metadata(self):
        with tempfile.TemporaryDirectory() as temporary:
            baseline = Path(temporary) / "Generated"
            baseline.mkdir()
            (baseline / "Stale.cs").write_text("stale\n")
            metadata = baseline / "Message.cs.meta"
            metadata.write_text("stable guid\n")

            def generate(_proto, output):
                (output / "Message.cs").write_text("generated\n")

            with patch.object(codegen, "GENERATED", baseline), patch.object(
                codegen, "generate", side_effect=generate
            ), patch("sys.argv", ["generate_csharp.py"]), redirect_stdout(
                io.StringIO()
            ):
                self.assertEqual(codegen.main(), 0)
            self.assertFalse((baseline / "Stale.cs").exists())
            self.assertEqual((baseline / "Message.cs").read_text(), "generated\n")
            self.assertEqual(metadata.read_text(), "stable guid\n")


if __name__ == "__main__":
    unittest.main()
