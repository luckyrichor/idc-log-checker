#!/usr/bin/env python3
import json
import shutil
import sys
from pathlib import Path


def write_valid(path):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(
        "\r\n".join(
            [
                "自动测试正常样本",
                "System CPU Using Percentage : 12%",
                "Memory Using Percentage: 20%",
                "Clock is synchronized, stratum 2",
                "System software version 1.0, uptime 10 days",
                "No active alarm",
                "BGP Total number of peers : 0",
                "Network routes advertised: 0",
                "OSPF process 10, 0 Neighbors, 0 is Full",
                "BFD neighbor Up",
                "sysname TEST-DEVICE",
                "Interface Eth1 AdminStatus UP OperStatus UP",
                "fan-id 1 status ok normal",
                "power id 1 status ok normal",
                "Temperature current 40 C threshold 80 C",
                "Transceiver RX Power: -5 dBm",
                "Flash usage: 10%",
                "Log count: 0",
            ]
        )
        + "\r\n",
        encoding="utf-8",
    )


def create_valid(root, devices):
    for device in devices:
        for filename in device["txtFiles"]:
            write_valid(root / device["name"] / filename)


def main():
    project_root = Path(sys.argv[1]).resolve()
    manifest_path = project_root / "src/Checker.Core/Baseline/manifest.json"
    output_root = project_root / "test/.generated"
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    devices = manifest["devices"]
    if output_root.exists():
        shutil.rmtree(str(output_root))
    output_root.mkdir(parents=True)

    case_ids = [
        "01-valid",
        "04-missing-directory",
        "07-missing-txt",
        "10-zero-byte",
        "13-one-line",
        "17-multiple-findings",
    ]
    for case_id in case_ids:
        root = output_root / case_id
        create_valid(root, devices)
        first_device = devices[0]
        first_name = first_device["name"]
        first_file = first_device["txtFiles"][0]
        if case_id == "04-missing-directory":
            shutil.rmtree(str(root / devices[-1]["name"]))
        elif case_id == "07-missing-txt":
            (root / first_name / first_file).unlink()
        elif case_id == "10-zero-byte":
            (root / first_name / first_file).write_bytes(b"")
        elif case_id == "13-one-line":
            (root / first_name / first_file).write_text("测试只有一行", encoding="utf-8")
        elif case_id == "17-multiple-findings":
            shutil.rmtree(str(root / devices[-1]["name"]))
            (root / first_name / first_file).write_bytes(b"")
            write_valid(root / first_name / "extra-test-file.txt")

    print("已生成 6 个可直接选择的完整测试用例：{}".format(output_root))


if __name__ == "__main__":
    main()
