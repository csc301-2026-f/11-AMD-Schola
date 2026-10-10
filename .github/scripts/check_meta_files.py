# Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.
"""Check that Unity .meta files under Unity/ are complete and not orphaned.

Run by checks.yml: ``python .github/scripts/check_meta_files.py``

Unity needs a ``.meta`` file next to every asset file and folder; it stores the asset's
GUID. A missing ``.meta`` breaks references for everyone else who pulls the change, and an
orphaned ``.meta`` (asset deleted, ``.meta`` left behind) produces import warnings.

TODO:
  1. Walk every file and folder under ``Unity/`` (start with ``Unity/com.amd.schola``).
  2. Skip entries Unity ignores: names starting with ``.`` (e.g. ``.gitkeep``), names ending
     with ``~`` (e.g. ``Samples~``, ``Documentation~``), and the package root folder itself.
  3. Missing: for each asset ``X`` (file or folder) report an error if ``X.meta`` does not exist.
  4. Orphaned: for each ``X.meta`` report an error if ``X`` does not exist.
  5. Print every problem as ``path: missing .meta`` / ``path: orphaned .meta`` and exit 1 if
     any were found, otherwise print a one-line success message and exit 0.
  6. Use only the standard library (pathlib, sys) so CI needs no installs.
"""

import sys


def main() -> int:
    print("Placeholder - .meta file check not implemented yet.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
