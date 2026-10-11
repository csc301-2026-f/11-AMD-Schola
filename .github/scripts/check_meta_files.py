# Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.
"""Check missing and orphaned Unity .meta files without external dependencies."""

from pathlib import Path
import sys

REPO_ROOT = Path(__file__).resolve().parents[2]


def ignored(name: str) -> bool:
    return name.startswith(".") or name.endswith("~")


def validate(unity_root: Path) -> list[str]:
    if not unity_root.is_dir():
        return [f"{unity_root}: Unity directory does not exist"]

    problems = []

    def walk(directory: Path) -> None:
        for entry in sorted(directory.iterdir()):
            asset_name = entry.name[:-5] if entry.name.endswith(".meta") else entry.name
            if ignored(asset_name):
                continue
            if entry.name.endswith(".meta"):
                if not entry.with_name(asset_name).exists():
                    problems.append(f"{entry}: orphaned .meta")
                continue
            # Direct child directories are package roots, which need no .meta.
            if not (directory == unity_root and entry.is_dir()):
                if not entry.with_name(entry.name + ".meta").is_file():
                    problems.append(f"{entry}: missing .meta")
            if entry.is_dir():
                walk(entry)

    walk(unity_root)
    return problems


def main() -> int:
    problems = validate(REPO_ROOT / "Unity")
    for problem in problems:
        print(problem)
    if problems:
        return 1
    print("Unity .meta files are complete and have no orphans.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
