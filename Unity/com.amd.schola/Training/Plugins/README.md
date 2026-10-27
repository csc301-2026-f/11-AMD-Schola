# Locally restored experiment dependencies

Run python Unity/tools/prepare_startup.py from the repository root before enabling
SCHOLA_STARTUP_SPIKE. DLLs, plugin importer metadata, and the restore manifest are
generated here and intentionally ignored by Git.

Pinned packages and expected hashes are in Unity/tools/startup-packages.lock.json.
The full instructions and limitations are in Unity/STARTUP-EXPERIMENT.md.

Grpc.Core / Grpc.Core.Api: Apache-2.0, https://github.com/grpc/grpc
Google.Protobuf: BSD-3-Clause, https://github.com/protocolbuffers/protobuf
System.Memory / System.Buffers / System.Numerics.Vectors /
System.Runtime.CompilerServices.Unsafe: MIT, https://github.com/dotnet/runtime

These binaries are not redistributed in this change. Package licenses remain
available through their NuGet package metadata/source repositories.

