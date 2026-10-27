# Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.
"""Restore the Windows x64 gRPC spike dependencies and generate C# from Schola schemas."""

import argparse
import hashlib
import json
from pathlib import Path
import subprocess
import sys
import urllib.request
import uuid
import zipfile

ROOT = Path(__file__).resolve().parents[2]
PACKAGE = ROOT / "Unity/com.amd.schola"
# Grpc.Core is a maintenance-mode transport used only for this isolated spike.
PACKAGES = {
    "grpc.core": ("2.46.6", "lib/netstandard2.0/Grpc.Core.dll"),
    "grpc.core.api": ("2.46.6", "lib/netstandard2.0/Grpc.Core.Api.dll"),
    "google.protobuf": ("3.31.1", "lib/netstandard2.0/Google.Protobuf.dll"),
    "system.memory": ("4.5.5", "lib/netstandard2.0/System.Memory.dll"),
    "system.buffers": ("4.5.1", "lib/netstandard2.0/System.Buffers.dll"),
    "system.runtime.compilerservices.unsafe": (
        "6.0.0",
        "lib/netstandard2.0/System.Runtime.CompilerServices.Unsafe.dll",
    ),
    "system.numerics.vectors": (
        "4.5.0",
        "lib/netstandard2.0/System.Numerics.Vectors.dll",
    ),
    "grpc.tools": ("2.72.0", None),
}
SCHEMAS = [
    "DType.proto",
    "Spaces.proto",
    "Points.proto",
    "Definitions.proto",
    "State.proto",
    "StateUpdates.proto",
    "GymConnector.proto",
]


def plugin_meta(path, native=False):
    # Stable IDs across restore runs. Explicit references keep these out of inference assemblies.
    guid = uuid.uuid5(uuid.NAMESPACE_URL, "amd-schola/startup/" + path.name).hex
    cpu = "x86_64" if native else "AnyCPU"
    text = f"""fileFormatVersion: 2
guid: {guid}
PluginImporter:
  externalObjects: {{}}
  serializedVersion: 2
  iconMap: {{}}
  executionOrder: {{}}
  defineConstraints: []
  isPreloaded: 0
  isOverridable: 0
  isExplicitlyReferenced: 1
  validateReferences: 1
  platformData:
  - first:
      Any:
    second:
      enabled: 0
      settings: {{}}
  - first:
      Editor: Editor
    second:
      enabled: 1
      settings:
        CPU: {cpu}
        DefaultValueInitialized: true
        OS: Windows
  - first:
      Standalone: Win64
    second:
      enabled: 1
      settings:
        CPU: {cpu}
  userData:
  assetBundleName:
  assetBundleVariant:
"""
    path.with_name(path.name + ".meta").write_text(text, encoding="utf-8")


def prepare(cache):
    cache.mkdir(parents=True, exist_ok=True)
    plugins = PACKAGE / "Training/Plugins"
    generated = PACKAGE / "Training/Generated"
    plugins.mkdir(parents=True, exist_ok=True)
    generated.mkdir(parents=True, exist_ok=True)
    hashes = {}
    expected_hashes = json.loads(
        (ROOT / "Unity/tools/startup-packages.lock.json").read_text()
    )
    for name, (version, assembly) in PACKAGES.items():
        archive = cache / f"{name}.{version}.nupkg"
        if not archive.exists():
            url = f"https://api.nuget.org/v3-flatcontainer/{name}/{version}/{archive.name}"
            print(f"Downloading {name} {version}", flush=True)
            urllib.request.urlretrieve(url, archive)
        hashes[archive.name] = hashlib.sha256(archive.read_bytes()).hexdigest()
        if hashes[archive.name] != expected_hashes[archive.name]:
            raise ValueError(
                f"SHA-256 mismatch for {archive}; remove it and retry the download."
            )
        with zipfile.ZipFile(archive) as package:
            if assembly:
                target = plugins / Path(assembly).name
                target.write_bytes(package.read(assembly))
                plugin_meta(target)
            if name == "grpc.core":
                target = plugins / "grpc_csharp_ext.x64.dll"
                target.write_bytes(
                    package.read("runtimes/win-x64/native/grpc_csharp_ext.x64.dll")
                )
                plugin_meta(target, native=True)
            if name == "grpc.tools":
                tools = cache / f"{name}.{version}"
                # Extract only known compiler files; no arbitrary archive paths.
                for tool in ("protoc.exe", "grpc_csharp_plugin.exe"):
                    member = f"tools/windows_x64/{tool}"
                    target = tools / tool
                    target.parent.mkdir(parents=True, exist_ok=True)
                    target.write_bytes(package.read(member))
    tools = cache / "grpc.tools.2.72.0"
    command = [
        str(tools / "protoc.exe"),
        f"--proto_path={ROOT / 'Proto'}",
        f"--csharp_out={generated}",
        f"--grpc_out={generated}",
        "--grpc_opt=no_client",
        f"--plugin=protoc-gen-grpc={tools / 'grpc_csharp_plugin.exe'}",
    ]
    subprocess.run(command + SCHEMAS, check=True)
    # Data-only protos generate empty service files; omit these for readability.
    for path in generated.glob("*Grpc.cs"):
        if path.name != "GymConnectorGrpc.cs":
            path.unlink()
    (plugins / "restored-packages.json").write_text(
        json.dumps(
            {"sha256": hashes, "versions": {k: v[0] for k, v in PACKAGES.items()}},
            indent=2,
        )
        + "\n",
        encoding="utf-8",
    )
    print("C# generation and Windows x64 plugin restore complete.", flush=True)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--cache", type=Path, default=ROOT / "Unity/.startup-cache")
    args = parser.parse_args()
    if sys.platform != "win32":
        parser.error("This first transport experiment supports Windows x64 only.")
    prepare(args.cache.resolve())
