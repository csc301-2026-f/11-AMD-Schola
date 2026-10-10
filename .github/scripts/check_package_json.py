# Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.
"""Validate the Unity package manifest at Unity/com.amd.schola/package.json.

Run by checks.yml: ``python .github/scripts/check_package_json.py``

TODO: report every failed check below, then exit 1 if any failed, else exit 0.
  1. The file exists and is valid JSON.
  2. ``name`` is exactly ``com.amd.schola``.
  3. ``version`` is semantic versioning ``MAJOR.MINOR.PATCH`` (optional ``-prerelease``).
  4. ``unity`` is present and looks like ``6000.3`` (``MAJOR.MINOR``).
  5. ``displayName`` and ``description`` are non-empty.
  6. If ``dependencies`` is present, every value is a version string.
  Use only the standard library (json, re, pathlib, sys).
"""

import sys


def main() -> int:
    print("Placeholder - package.json check not implemented yet.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
