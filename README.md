# IDC 日志检查工具

用于 Windows 11 64 位环境的设备目录、命令文件名和 TXT 执行结果检查工具。

## 内容

- `src/`：Checker.Core、Avalonia 和 WinForms 源码
- `scripts/`：构建、发布和 Mac 测试脚本
- `tests/`：自动化测试
- `test/`：测试说明、测试场景和完整测试用例
- `docs/`：设计与实现文档
- `release/`：Windows 使用说明和发布文件校验清单

Windows 可执行程序作为 GitHub Release 附件发布，不放入 Git 历史，以避免超过 GitHub 普通 Git 文件限制。

本仓库不包含真实日志、设备清单 Excel、开发环境缓存和构建中间文件。

## 使用

从 GitHub Releases 下载 Windows 版本，优先运行 Avalonia 版；解压测试用例后，可在程序中选择或拖入测试目录进行检查。

## 当前验证

项目当前自动化测试共 184 项，发布前全部通过。
