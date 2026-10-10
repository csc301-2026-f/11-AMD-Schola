# Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.
"""End-to-end smoke test: drive a running headless Unity build with Schola's Python client.

Run by smoke-test.yml: ``python .github/scripts/unity_smoke_test.py --port <port>``
Not needed until training over gRPC works.

Kept out of Test/ on purpose: pytest.ini collects everything under Test/, and this script
needs a running Unity build.

TODO:
  1. Parse ``--port`` (the one the build job uses), ``--steps`` (default ~500) and
     ``--timeout`` (seconds to wait for the build to accept connections).
  2. Retry connecting until the build is up or the timeout expires.
  3. Create the environment through Schola's existing Python API (the same path as
     Test/integration/train_test.py) - do not add Unity-specific code to Resources/python.
  4. reset(), then step with random valid actions for ``--steps`` steps, resetting on
     terminated/truncated.
  5. Assert observations match the observation space and rewards are finite numbers.
  6. Exit 1 with a clear message on any exception, timeout or failed assertion; exit 0 otherwise.
"""

import sys


def main() -> int:
    print("Placeholder - Unity smoke test not implemented yet.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
