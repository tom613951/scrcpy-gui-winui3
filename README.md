# Scrcpy GUI (WinUI 3 版)

这是一个使用 C# 和 WinUI 3 开发的现代 scrcpy 图形化控制面板。

## 🌟 项目特色

- 🎨 **WinUI 3 风格界面**：沉浸式单页架构，采用 Windows 11 Fluent Design 原生卡片化布局。
- ⚙️ **自定义核心路径**：轻量化体积（仅 30+MB），不再内置下载器，用户需自行前往系统设置指定 scrcpy 的安装路径。
- 📱 **设备与无线连接**：支持 USB 和无线 ADB 连接管理，包含一键终止 ADB 和 Android 11+ 无线配对助手。
- 🎮 **三大投屏模式**：提供原生屏幕镜像、直接调用相机（Camera）和创建虚拟独立显示器（Desktop）。
- ⌨️ **原生体验增强**：支持开启 UHID 模拟物理键盘和鼠标直通，提供低延迟电竞级游戏体验。
- 📂 **拖拽传输支持**：拖入 APK 自动静默安装，拖入文件自动推送到 `/sdcard/Download`。
- 🖥️ **参数配置与日志**：提供直观的比特率、分辨率和帧率设置，并内嵌实时控制台日志窗口。

## 🚀 快速开始

1. **环境准备**：此精简版需在电脑上预先安装 [.NET 9 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)。
2. **下载与运行**：前往 [Releases 页面](https://github.com/tom613951/scrcpy-gui-winui3/releases) 下载最新的 `scrcpy-gui-winui3-portable.zip`，解包到本地目录，双击运行 `ScrcpyGui.exe`。
3. **配置核心组件**：首次运行后，请点击主页的“系统设置”，指定您的 `scrcpy` 核心组件所在文件夹。如果您的 `adb.exe` 不在 `scrcpy` 目录下，您也可以在设置中单独指定自定义的 `adb` 路径。

## 📝 许可证

本项目基于 [MIT License](LICENSE) 开源。
