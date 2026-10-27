# Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.
"""Run the exact server core outside Unity; this does not validate Unity imports or Play mode."""

import argparse
from pathlib import Path
import queue
import subprocess
import sys
import threading

ROOT = Path(__file__).resolve().parents[2]
HARNESS = ROOT / "Unity/tools/StartupHarness/bin/Debug/net8.0/StartupHarness.dll"
PROBE = ROOT / "Unity/tools/probe_startup.py"
FLAGS = subprocess.CREATE_NO_WINDOW if sys.platform == "win32" else 0


def launch(dotnet, port):
    process = subprocess.Popen(
        [dotnet, str(HARNESS), str(port)],
        stdin=subprocess.PIPE,
        stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT,
        text=True,
        creationflags=FLAGS,
    )
    lines = queue.Queue()

    def read_ready():
        for line in process.stdout:
            if line.startswith("READY "):
                lines.put(line)
                return
            print(line, end="", flush=True)
        lines.put(None)

    threading.Thread(target=read_ready, daemon=True).start()
    try:
        line = lines.get(timeout=15)
        if line is None:
            raise RuntimeError("Server exited before readiness")
        return process, int(line.split()[1])
    except BaseException:
        process.kill()
        process.communicate()
        raise


def stop(process):
    output, _ = process.communicate("\n", timeout=15)
    print(output, end="", flush=True)
    if process.returncode != 0 or "STARTUPS 6" not in output:
        raise AssertionError("Expected six startup calls and successful cleanup")


def main(dotnet):
    lifecycle = subprocess.run(
        [dotnet, str(HARNESS), "--self-test"],
        capture_output=True,
        text=True,
        timeout=20,
        creationflags=FLAGS,
    )
    print(lifecycle.stdout, end="", flush=True)
    if lifecycle.returncode != 0:
        raise RuntimeError(lifecycle.stderr)
    port = 0
    for attempt in range(2):
        process, port = launch(dotnet, port)
        try:
            result = subprocess.run(
                [sys.executable, str(PROBE), "--port", str(port)],
                capture_output=True,
                text=True,
                timeout=30,
                creationflags=FLAGS,
            )
            print(result.stdout, end="", flush=True)
            if result.returncode != 0:
                raise RuntimeError(result.stderr)
            if attempt == 0:
                collision = subprocess.run(
                    [dotnet, str(HARNESS), str(port)],
                    capture_output=True,
                    text=True,
                    timeout=15,
                    creationflags=FLAGS,
                )
                if collision.returncode == 0:
                    raise AssertionError(
                        "Second listener unexpectedly acquired the occupied port"
                    )
                print("PASS occupied port rejected", flush=True)
            stop(process)
        finally:
            if process.poll() is None:
                process.kill()
                process.communicate()
        print(f"PASS server lifecycle {attempt + 1}; port {port}", flush=True)
    offline = subprocess.run(
        [
            sys.executable,
            str(PROBE),
            "--port",
            str(port),
            "--timeout",
            "0.25",
            "--repeats",
            "1",
        ],
        capture_output=True,
        text=True,
        timeout=10,
        creationflags=FLAGS,
    )
    if offline.returncode == 0:
        raise AssertionError("Offline endpoint unexpectedly passed")
    print("PASS stopped endpoint fails within the deadline", flush=True)
    print("TRANSPORT CHECKS PASSED (Unity Play mode still unverified)", flush=True)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--dotnet", default="dotnet")
    main(parser.parse_args().dotnet)
