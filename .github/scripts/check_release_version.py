# Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.
"""Check that a release tag matches the Unity package version.

Run by release.yml: ``python .github/scripts/check_release_version.py "${GITHUB_REF_NAME}"``

TODO:
  1. Read the tag from ``sys.argv[1]`` (e.g. ``unity-v0.1.0``); fail with a usage message if missing.
  2. Require the ``unity-v`` prefix and strip it to get the expected version.
  3. Read ``version`` from ``Unity/com.amd.schola/package.json``.
  4. Exit 1 with a clear message if they differ
     (e.g. "tag unity-v0.2.0 does not match package.json version 0.1.0"), else exit 0.
  Use only the standard library.
"""

import sys


def main() -> int:
    print("Placeholder - release version check not implemented yet.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
