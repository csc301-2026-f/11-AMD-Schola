# Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.
"""Generate Unity protobuf/gRPC C# sources, or check a committed baseline.

Requires Python 3.11 and network access to NuGet; no Unity license or .NET SDK.
"""

import argparse
import difflib
import hashlib
import io
from pathlib import Path
import platform
import subprocess
import sys
import tempfile
import urllib.request
import zipfile

REPO_ROOT = Path(__file__).resolve().parents[2]
GENERATED = REPO_ROOT / "Unity/com.amd.schola/Training/Generated"
GRPC_TOOLS_VERSION = "2.67.0"
GRPC_TOOLS_SHA256 = "9acfe56d6c1bf54b891cd9774f95f6980ba5087850ded122a912d605f518a15d"
REGENERATE = "python .github/scripts/generate_csharp.py"


def tool_platform() -> str:
    system = platform.system()
    machine = platform.machine().lower()
    if system == "Darwin":
        # Grpc.Tools ships macOS x64 binaries; Apple Silicon requires Rosetta 2.
        return "macosx_x64"
    architecture = {
        "x86_64": "x64",
        "amd64": "x64",
        "aarch64": "arm64",
        "arm64": "arm64",
    }.get(machine)
    if system == "Linux" and architecture in ("x64", "arm64"):
        return f"linux_{architecture}"
    if system == "Windows" and architecture == "x64":
        return "windows_x64"
    raise RuntimeError(f"Unsupported Grpc.Tools platform: {system} {machine}")


def generate(proto_root: Path, output: Path) -> None:
    sources = sorted(
        path.relative_to(proto_root).as_posix() for path in proto_root.rglob("*.proto")
    )
    if not sources:
        raise RuntimeError(f"No .proto files found under {proto_root}")
    target = tool_platform()
    url = (
        f"https://api.nuget.org/v3-flatcontainer/grpc.tools/{GRPC_TOOLS_VERSION}/"
        f"grpc.tools.{GRPC_TOOLS_VERSION}.nupkg"
    )
    with urllib.request.urlopen(url, timeout=60) as response:
        package = response.read()
    if hashlib.sha256(package).hexdigest() != GRPC_TOOLS_SHA256:
        raise RuntimeError("Grpc.Tools package checksum mismatch")

    output.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory(prefix="schola-grpc-tools-") as temporary:
        tools = Path(temporary)
        suffix = ".exe" if target.startswith("windows") else ""
        with zipfile.ZipFile(io.BytesIO(package)) as archive:
            for name in ("protoc", "grpc_csharp_plugin"):
                binary = tools / (name + suffix)
                binary.write_bytes(archive.read(f"tools/{target}/{name}{suffix}"))
                binary.chmod(0o755)
        subprocess.run(
            [
                str(tools / ("protoc" + suffix)),
                f"--proto_path={proto_root}",
                f"--csharp_out={output}",
                f"--grpc_out={output}",
                f"--plugin=protoc-gen-grpc={tools / ('grpc_csharp_plugin' + suffix)}",
                *sources,
            ],
            cwd=proto_root,
            check=True,
        )


def compare(expected: Path, actual: Path) -> bool:
    """Compare the whole C# file set, including untracked/missing/stale files."""
    expected_files = {p.relative_to(expected) for p in expected.rglob("*.cs")}
    actual_files = {p.relative_to(actual) for p in actual.rglob("*.cs")}
    matches = True
    for name in sorted(expected_files | actual_files):
        if name not in actual_files:
            print(f"Missing generated file: {name}")
        elif name not in expected_files:
            print(f"Stale generated file: {name}")
        elif (expected / name).read_bytes() == (actual / name).read_bytes():
            continue
        else:
            print(
                "".join(
                    difflib.unified_diff(
                        (actual / name).read_text().splitlines(keepends=True),
                        (expected / name).read_text().splitlines(keepends=True),
                        fromfile=f"committed/{name}",
                        tofile=f"regenerated/{name}",
                    )
                ),
                end="",
            )
        matches = False
    return matches


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--check", action="store_true", help="Compare without modifying committed files"
    )
    parser.add_argument(
        "--output", type=Path, default=GENERATED, help="Generated C# directory"
    )
    args = parser.parse_args()
    destination_root = args.output.resolve()
    with tempfile.TemporaryDirectory(prefix="schola-csharp-") as temporary:
        output = Path(temporary)
        generate(REPO_ROOT / "Proto", output)
        if args.check:
            if not compare(output, destination_root):
                print(f"Generated C# is out of date. Run: {REGENERATE}")
                return 1
            print("Generated C# matches Proto/.")
        else:
            destination_root.mkdir(parents=True, exist_ok=True)
            for stale in destination_root.rglob("*.cs"):
                stale.unlink()
            for source in output.rglob("*.cs"):
                destination = destination_root / source.relative_to(output)
                destination.parent.mkdir(parents=True, exist_ok=True)
                destination.write_bytes(source.read_bytes())
            print(
                f"Generated C# in {destination_root}. "
                "Import in Unity and commit sources and .meta files."
            )
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except (
        OSError,
        RuntimeError,
        subprocess.CalledProcessError,
        zipfile.BadZipFile,
    ) as error:
        print(f"C# generation failed: {error}", file=sys.stderr)
        sys.exit(1)
