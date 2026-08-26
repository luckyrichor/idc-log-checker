#!/usr/bin/env python3
import hashlib
import sys
from pathlib import Path


def sha256(path):
    digest = hashlib.sha256()
    with path.open("rb") as source:
        for chunk in iter(lambda: source.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


release_dir = Path(sys.argv[1]).resolve()
names = ["IDC日志检查工具_Avalonia.exe", "IDC日志检查工具_WinForms.exe"]
lines = ["设备检查工具发布文件 SHA-256", "==============================", ""]
for name in names:
    path = release_dir / name
    lines.extend([name, sha256(path), ""])
with (release_dir / "SHA256.txt").open("w", encoding="utf-8", newline="\n") as output:
    output.write("\n".join(lines))
