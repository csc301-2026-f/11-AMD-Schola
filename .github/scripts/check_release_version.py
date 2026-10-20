# Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.
"""Check that a release tag matches the Unity package version.

Run by release.yml: ``python .github/scripts/check_release_version.py "${GITHUB_REF_NAME}"``

The tag must be ``unity-v<version>`` (for example ``unity-v0.1.0``) and ``<version>`` must
equal ``version`` in ``Unity/com.amd.schola/package.json``. Standard library only.
"""

import json
from pathlib import Path
import sys

import check_package_json

MANIFEST = Path(__file__).resolve().parents[2] / "Unity/com.amd.schola/package.json"
TAG_PREFIX = "unity-v"
USAGE = "usage: check_release_version.py <tag>   (for example unity-v0.1.0)"


def validate(tag: str, manifest_path: Path) -> list[str]:
    """Return a list of problems; an empty list means the tag matches the package."""
    if not tag.startswith(TAG_PREFIX):
        return [f"tag {tag} must start with {TAG_PREFIX}"]
    tag_version = tag[len(TAG_PREFIX) :]
    if not check_package_json.version_string(tag_version):
        return [
            f"tag {tag} does not end in a valid version (expected MAJOR.MINOR.PATCH "
            "with optional prerelease)"
        ]

    try:
        manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    except (OSError, UnicodeError, json.JSONDecodeError) as error:
        return [f"{manifest_path}: cannot read valid JSON: {error}"]
    if not isinstance(manifest, dict):
        return [f"{manifest_path}: manifest must be a JSON object"]

    package_version = manifest.get("version")
    if package_version != tag_version:
        return [f"tag {tag} does not match package.json version {package_version}"]
    return []


def main(argv: list[str] | None = None) -> int:
    args = sys.argv[1:] if argv is None else argv
    if len(args) != 1 or not args[0]:
        print(USAGE)
        return 1
    tag = args[0]

    problems = validate(tag, MANIFEST)
    for problem in problems:
        print(problem)
    if problems:
        return 1
    print(f"Tag {tag} matches package.json version.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
