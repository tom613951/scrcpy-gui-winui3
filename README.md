# Scrcpy GUI (WinUI 3 版)

这是一个使用 **C#**、**.NET 10** 和 **WinUI 3 (Windows App SDK)** 重新构建的现代 **scrcpy** 图形化控制面板。

本项目的界面设计灵感来自于 **UniGetUI**，采用了极致流畅的 **Windows 11 Fluent Design** 风格，提供原生 Mica 亚克力背景、卡片化布局、响应式设计以及丝滑的微动画，彻底告别传统 GUI 粗糙简陋的界面风格。

---

## 🌟 项目特色

- 🎨 **现代化 UI (UniGetUI 风格)**：
  - 完美适配 Windows 11 原生主题，支持 **Mica（云母）** 背景与暗黑模式。
  - 采用优雅的卡片流布局展示设备和配置，美观大方。
- ⚡ **开箱即用 (自动托管 scrcpy)**：
  - 首次运行支持**自动从 Genymobile 官方 GitHub 镜像下载**最新版 `scrcpy` 二进制包并解压托管，无需手动配置环境变量。
  - 同时提供**手动指定 scrcpy 路径**的选项，满足自定义开发调试需求。
- 📱 **多设备管理卡片**：
  - 以卡片形式清晰展示所有已连接的 ADB 设备（USB / Wi-Fi）。
  - 每个设备卡片集成一键投屏、快捷无线连接、终端日志查看等操作。
- 📶 **便捷的无线配对与连接**：
  - 支持通过 IP 地址一键连接无线 ADB。
  - 内置 **Android 11+ 无线配对 (Pairing)** 助手，输入配对码即可轻松完成无线调试配置。
- 🖥️ **完整参数配置面版**：
  - 图形化配置比特率 (Bitrate)、分辨率缩放、帧率限制。
  - 支持开启/关闭音频传输、保持屏幕常亮、只读投屏等数十项高级选项。
- 📄 **内嵌终端日志**：
  - 界面底部内置实时控制台终端，能够实时截获并展示 `scrcpy` 和 `adb` 的进程日志输出，排查问题更直观。

---

## 📸 界面截图

*(以下为应用主要界面展示)*

| 设备管理界面 | 系统参数设置 | 自动下载页面 |
| :---: | :---: | :---: |
| 设备卡片列表、无线配对面板与实时控制台日志 | 画面清晰度、音频传输及自定义路径配置卡片 | 托管包下载进度与自动解压配置 |

---

## 🚀 快速开始

### 运行环境要求
- **操作系统**：Windows 10 Build 17763 (1809) 或更高版本 (推荐 Windows 11)。
- **依赖组件**：本地需装有 [.NET 9 Runtime](https://dotnet.microsoft.com/download/dotnet/9.0)（如果下载了 `Self-Contained` 独立运行版，则无需额外安装 .NET 运行时）。

### 安装与运行 (便携版)
1. 前往本仓库的 [Releases 发行版页面](https://github.com/tom613951/scrcpy-gui-winui3/releases)。
2. 下载最新的 `scrcpy-gui-winui3-v1.0.0-x64-Portable.zip`。
3. 解压压缩包到任意非中文路径文件夹中。
4. 双击运行 `ScrcpyGui.exe` 即可开始使用。

---

## 🛠️ 核心架构

项目采用标准的 **MVVM (Model-View-ViewModel)** 架构构建，通过依赖注入管理核心服务：

- **Services/AdbService.cs**：负责检测 ADB 设备、发起无线配对和连接命令。
- **Services/ScrcpyService.cs**：解析用户界面设定的参数，装配完整的 `scrcpy` 命令行并拉起子进程。
- **Services/UpdateService.cs**：通过 GitHub API 查询 `Genymobile/scrcpy` 的最新 Windows x64 版本，负责断点续传下载并使用 `Tar` 解包。
- **Services/SettingsService.cs**：本地参数持久化，存储于 `Appdata\Local\ScrcpyGui\settings.json`。

---

## 🤝 参与贡献

如果你在使用过程中遇到任何 Bug 或有新的功能建议，欢迎提交 **Issues** 或发起 **Pull Request**！

1. Fork 本仓库。
2. 创建你的特性分支 (`git checkout -b feature/AmazingFeature`)。
3. 提交你的修改 (`git commit -m 'Add some AmazingFeature'`)。
4. 推送到分支 (`git push origin feature/AmazingFeature`)。
5. 提交 Pull Request。

---

## 📝 许可证

本项目基于 [MIT License](LICENSE) 开源。同时请遵循 [scrcpy](https://github.com/Genymobile/scrcpy) 项目的开源协议。
