# Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.
"""Validate the Unity package manifest using only the standard library."""

import json
from pathlib import Path
import re
import sys

MANIFEST = Path(__file__).resolve().parents[2] / "Unity/com.amd.schola/package.json"
# SemVer core and optional prerelease; numeric identifiers cannot have leading zeros.
NUMBER = r"(?:0|[1-9][0-9]*)"
IDENTIFIER = rf"(?:{NUMBER}|[0-9A-Za-z-]*[A-Za-z-][0-9A-Za-z-]*)"
VERSION = re.compile(
    rf"{NUMBER}\.{NUMBER}\.{NUMBER}(?:-{IDENTIFIER}(?:\.{IDENTIFIER})*)?"
)


def version_string(value: object) -> bool:
    return isinstance(value, str) and VERSION.fullmatch(value) is not None


def validate(path: Path) -> list[str]:
    try:
        manifest = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, UnicodeError, json.JSONDecodeError) as error:
        return [f"{path}: cannot read valid JSON: {error}"]
    if not isinstance(manifest, dict):
        return [f"{path}: manifest must be a JSON object"]

    problems = []
    if manifest.get("name") != "com.amd.schola":
        problems.append("name must be com.amd.schola")
    if not version_string(manifest.get("version")):
        problems.append("version must be MAJOR.MINOR.PATCH with optional prerelease")
    unity = manifest.get("unity")
    if not isinstance(unity, str) or re.fullmatch(r"[0-9]+\.[0-9]+", unity) is None:
        problems.append("unity must be a MAJOR.MINOR string")
    for field in ("displayName", "description"):
        value = manifest.get(field)
        if not isinstance(value, str) or not value.strip():
            problems.append(f"{field} must be a non-empty string")
    if "dependencies" in manifest:
        dependencies = manifest["dependencies"]
        if not isinstance(dependencies, dict):
            problems.append("dependencies must be a JSON object")
        else:
            for name, version in dependencies.items():
                if not version_string(version):
                    problems.append(f"dependencies.{name} must be a version string")
    return [f"{path}: {problem}" for problem in problems]


def main() -> int:
    problems = validate(MANIFEST)
    for problem in problems:
        print(problem)
    if problems:
        return 1
    print("Unity package manifest is valid.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
