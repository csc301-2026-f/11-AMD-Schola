# Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.
"""Check missing and orphaned Unity .meta files without external dependencies.

AGENT TODO (blocking before Unity/SmokeTestProject is committed):
  Today every folder directly under Unity/ is treated as a UPM package root, and the
  walker requires a .meta next to every non-ignored file and folder. A real Unity
  project (SmokeTestProject for the smoke-test CI jobs) has ProjectSettings/ and
  Packages/ trees that never have .meta files, so committing that project will make
  checks.yml fail until this script is updated.

  Required behavior:
  1. Treat a Unity/ child as a Unity *project* when it contains ProjectSettings/
     (detect via ProjectSettings/ProjectVersion.txt or a ProjectSettings/ directory).
  2. For a project, only validate Assets/ for missing/orphaned .meta files.
  3. Do not require .meta under ProjectSettings/, Packages/, Library/, Temp/, Logs/,
     obj/, UserSettings/, or other generated project folders.
  4. Keep the current package-root behavior for UPM packages such as
     Unity/com.amd.schola (no ProjectSettings/).
  5. Add a regression case in .github/scripts/tests/test_checks.py that builds a
     fake Unity/SmokeTestProject with ProjectSettings/ProjectVersion.txt and
     Packages/manifest.json (no .meta) plus Assets/Foo.cs with Foo.cs.meta, and
     asserts validate() returns [].

  Related: .github/workflows/smoke-test.yml (build/run stay skipped until the project
  exists; do not commit the project until this TODO is done).
"""

from pathlib import Path
import sys

REPO_ROOT = Path(__file__).resolve().parents[2]


def ignored(name: str) -> bool:
    return name.startswith(".") or name.endswith("~")


def validate(unity_root: Path) -> list[str]:
    if not unity_root.is_dir():
        return [f"{unity_root}: Unity directory does not exist"]

    problems = []

    def walk(directory: Path) -> None:
        for entry in sorted(directory.iterdir()):
            asset_name = entry.name[:-5] if entry.name.endswith(".meta") else entry.name
            if ignored(asset_name):
                continue
            if entry.name.endswith(".meta"):
                if not entry.with_name(asset_name).exists():
                    problems.append(f"{entry}: orphaned .meta")
                continue
            # Direct child directories are package roots, which need no .meta.
            # AGENT TODO: when a Unity/ child is a project (has ProjectSettings/),
            # skip walking ProjectSettings/ and Packages/; only walk Assets/.
            if not (directory == unity_root and entry.is_dir()):
                if not entry.with_name(entry.name + ".meta").is_file():
                    problems.append(f"{entry}: missing .meta")
            if entry.is_dir():
                walk(entry)

    walk(unity_root)
    return problems


def main() -> int:
    problems = validate(REPO_ROOT / "Unity")
    for problem in problems:
        print(problem)
    if problems:
        return 1
    print("Unity .meta files are complete and have no orphans.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
