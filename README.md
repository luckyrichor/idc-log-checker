# IDC 日志检查工具

面向 Windows 11 x64 的桌面工具，用于核对 IDC 网络设备巡检导出的日志目录：**目录和文件是否齐全、命名是否正确、每个 TXT 里的命令是否真的执行成功**。

设计前提是使用者不熟悉计算机操作 —— 选一个目录、点「开始检查」，就能拿到可导出的中文结论。

## 解决什么问题

一次巡检会导出 62 个设备目录，每个目录下约 62 个 TXT（每个 TXT 是一条设备命令的执行结果）。人工核对近四千个文件既慢又容易漏。更麻烦的是：**文件存在不等于命令成功** —— 权限不足、超时、连接失败、参数写错，都会生成一个看似正常的 TXT，里面却没有有效输出。

本工具做三层检查：

| 层级 | 检查内容 |
|---|---|
| **结构层** | 设备目录是否缺失/多余、大小写是否一致、有无嵌套目录或非 TXT 文件 |
| **文件层** | TXT 是否缺失/多余、是否空文件、是否只有一行、能否读取 |
| **内容层** | 命令是否被识别、执行是否成功、输出是否有实际内容，以及具体设备指标是否异常 |

内容层是这个工具真正值钱的地方。它会识别命令类型（CPU、内存、时钟、NTP、版本、告警、BGP、OSPF、BFD、接口、风扇、电源、温度、光模块、存储、日志、配置等），然后按类型套用不同规则，报出这些问题：

- **执行类**：命令未识别、输入非法、命令不完整、参数过多、权限拒绝、超时、连接失败、无有效输出、输出无法识别
- **业务类**：BGP 邻居/地址族/Peer 未找到、CPU 占用过高、内存占用过高、NTP 未同步、严重告警、主要告警

## 基准清单

检查依据是一份**内置基准**（`src/Checker.Core/Baseline/manifest.json`），编译进 EXE，不需要外部配置文件：

- 62 个设备目录，每个目录带完整的 TXT 文件名清单
- 由巡检快照 `LogRst_20260823_2359` 生成，并与 `LogRst_20260801_0004` 交叉核对，两批完全一致
- 带 `baselineSha256` 校验值，防止被意外改动

基准需要更新时用 `scripts/generate-manifest.py` 重新生成。

## 项目结构

```
src/
├── Checker.Core/          检查核心，不依赖任何界面框架
│   ├── Baseline/          内置基准清单与校验
│   ├── Scanning/          目录扫描、问题分类（IssueCode / IssueSeverity）
│   ├── ContentAnalysis/   命令识别与内容校验（本项目最复杂的部分）
│   ├── Batch/             批量检查多个目录
│   ├── Reporting/         中文报告生成（单次 / 批量）
│   └── Presentation/      结果展示与「打开所在位置」
├── Checker.Avalonia/      跨平台界面，可在 Mac 上开发调试，发布 Windows EXE
└── Checker.WinForms/      Windows 原生界面，复用同一个 Core

tests/Checker.Tests/       32 个测试文件，自动构造测试目录验证规则
test/                      测试说明、测试场景、完整测试用例（含 Windows 用例 zip）
docs/superpowers/          设计文档（specs）与实施计划（plans）
scripts/                   构建、发布、Mac 测试、基准生成、校验和写入
release/                   使用说明与发布文件 SHA256 清单
```

**两套界面是刻意的**：Avalonia 版可以在 Mac 上真实运行，开发迭代快；WinForms 版是 Windows 原生，作为兼容性备选。两者共用 `Checker.Core`，检查逻辑只有一份。

## 构建与运行

需要 .NET SDK 10.0.400（见 `global.json`，`rollForward: latestPatch`）。

```bash
dotnet test                                    # 运行全部测试
dotnet run --project src/Checker.Avalonia      # 本地跑 Avalonia 版
bash scripts/publish-windows.sh                # 发布 Windows 自包含单文件 EXE
```

Mac 上没有 .NET SDK 时可用 `scripts/install-dotnet.sh` 装便携版（装在项目内，不污染系统）。

编译选项在 `Directory.Build.props`：**`TreatWarningsAsErrors` 开启**，`Nullable` 开启，确定性构建。改代码后有警告就过不了编译。

## 使用

从 [Releases](https://github.com/luckyrichor/idc-log-checker/releases) 下载 Windows 版本，**优先用 Avalonia 版**。解压测试用例后，在程序里选择或拖入目录即可检查。

产物是自包含单文件 EXE，不需要装 .NET、Python、数据库或 DLL。首次运行未签名 EXE 时 Windows 可能提示「未知发布者」；单文件运行会在 `%TEMP%` 产生缓存，这不是安装。

## 仓库边界

- **不含**真实日志、设备清单 Excel、构建中间产物和开发环境缓存
- Windows EXE 作为 **GitHub Release 附件**发布，不进 Git 历史 —— 自包含单文件体积大，会超出 GitHub 普通文件限制
- `release/SHA256.txt` 是发布件校验清单，用 `scripts/write-release-checksums.py` 生成

## 当前状态

自动化测试 184 项，发布前全部通过。
