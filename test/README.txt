IDC 日志检查工具测试目录
========================

本目录中的内容只用于测试，不会修改上级目录中的两份真实日志。

一、快速测试
1. 在 macOS 终端进入项目根目录。
2. 运行：test/automated/run-all-tests.sh
3. 运行：test/automated/generate-cases.sh

二、手工界面测试
生成后，test/.generated/ 下会出现 6 个完整的 62 目录测试用例：
- 01-valid：应无错误、无提示。
- 04-missing-directory：应报告缺少一个设备目录。
- 07-missing-txt：应报告缺少一个 TXT 文件。
- 10-zero-byte：应报告一个 TXT 文件为空。
- 13-one-line：应通过，但提示一个 TXT 文件只有一行。
- 17-multiple-findings：应同时报告缺目录、多文件、空文件等问题。

这些用例中的 TXT 均为测试内容，不是真实日志内容。完整 18 类场景见
test/cases/case-catalog.json，全部由自动化测试覆盖。
