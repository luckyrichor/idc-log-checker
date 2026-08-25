#!/usr/bin/env python3
"""Generate and cross-check the embedded directory/file-name baseline."""

from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path


def snapshot(root: Path) -> dict[str, list[str]]:
    if not root.is_dir():
        raise SystemExit(f"基准目录不存在: {root}")

    result: dict[str, list[str]] = {}
    for device_dir in sorted((p for p in root.iterdir() if p.is_dir()), key=lambda p: p.name):
        files = sorted((p.name for p in device_dir.iterdir() if p.is_file()))
        non_txt = [name for name in files if not name.lower().endswith(".txt")]
        if non_txt:
            raise SystemExit(f"发现非 TXT 基准文件: {device_dir.name}: {non_txt}")
        result[device_dir.name] = files
    return result


def mismatch_details(left: dict[str, list[str]], right: dict[str, list[str]]) -> list[str]:
    details: list[str] = []
    left_dirs = set(left)
    right_dirs = set(right)
    for name in sorted(left_dirs - right_dirs):
        details.append(f"仅第一批有目录: {name}")
    for name in sorted(right_dirs - left_dirs):
        details.append(f"仅第二批有目录: {name}")
    for name in sorted(left_dirs & right_dirs):
        left_files = set(left[name])
        right_files = set(right[name])
        for file_name in sorted(left_files - right_files):
            details.append(f"{name}: 仅第一批有文件: {file_name}")
        for file_name in sorted(right_files - left_files):
            details.append(f"{name}: 仅第二批有文件: {file_name}")
    return details


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("first", type=Path)
    parser.add_argument("second", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()

    first = snapshot(args.first)
    second = snapshot(args.second)
    details = mismatch_details(first, second)
    if details:
        raise SystemExit("两批基准不一致:\n" + "\n".join(details))

    devices = [
        {"name": name, "txtFiles": file_names}
        for name, file_names in first.items()
    ]
    canonical = json.dumps(devices, ensure_ascii=False, separators=(",", ":"), sort_keys=True)
    manifest = {
        "schemaVersion": 1,
        "sourceSnapshots": [args.first.name, args.second.name],
        "baselineSha256": hashlib.sha256(canonical.encode("utf-8")).hexdigest(),
        "devices": devices,
    }

    args.output.parent.mkdir(parents=True, exist_ok=True)
    with args.output.open("w", encoding="utf-8", newline="\n") as output_file:
        output_file.write(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n")
    total = sum(len(files) for files in first.values())
    print(f"已生成: {args.output}")
    print(f"设备目录: {len(first)}")
    print(f"TXT 文件名: {total}")
    print(f"SHA-256: {manifest['baselineSha256']}")


if __name__ == "__main__":
    main()
