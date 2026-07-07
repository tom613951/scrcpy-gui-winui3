# Scrcpy GUI (WinUI 3 版)

这是一个使用 C# 和 WinUI 3 开发的现代 scrcpy 图形化控制面板。

## 🌟 项目特色

- 🎨 **WinUI 3 风格界面**：沉浸式单页架构，采用 Windows 11 Fluent Design 原生卡片化布局。
- ⚙️ **自定义核心路径**：轻量化体积（仅 30+MB），不再内置下载器，用户需自行前往系统设置指定 scrcpy 的安装路径。
- 📱 **设备与无线连接**：支持 USB 和无线 ADB 连接管理，包含 ADB 服务重启、退出自动关闭 ADB 和 Android 11+ 无线配对助手。
- 🎮 **三大投屏模式**：提供原生屏幕镜像、直接调用相机（Camera）和创建虚拟独立显示器（Desktop）。
- ⌨️ **原生体验增强**：支持开启 UHID 模拟物理键盘和鼠标直通，提供低延迟电竞级游戏体验。
- 📂 **拖拽传输支持**：拖入 APK 自动静默安装，拖入文件自动推送到 `/sdcard/Download`。
- 🖥️ **参数配置与日志**：提供直观的比特率、分辨率和帧率设置，并内嵌实时控制台日志窗口。

## 🚀 快速开始

1. **环境准备**：此精简版需在电脑上预先安装 [.NET 9 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)。如果程序启动时报 WinUI/Windows App Runtime 相关错误，请同时安装 Microsoft Windows App Runtime。
2. **下载与运行**：前往 [Releases 页面](https://github.com/tom613951/scrcpy-gui-winui3/releases) 下载最新的 `scrcpy-gui-winui3-portable.zip`，解包到本地目录，双击运行 `ScrcpyGui.exe`。
3. **配置核心组件**：首次运行后，请点击主页的“系统设置”，指定您的 `scrcpy` 核心组件所在文件夹。如果您的 `adb.exe` 不在 `scrcpy` 目录下，您也可以在设置中单独指定自定义的 `adb` 路径。

## 💡 使用提示

- 程序退出时会自动停止当前配置路径下的 ADB 服务，并清理同一路径残留的 `adb.exe` 进程。
- 如果设备列表异常，可点击“重启 ADB 服务”重新初始化 ADB 并刷新设备列表。
- 如果电脑上安装了多个 ADB，建议在“系统设置”中明确指定要使用的 `adb.exe`，避免不同工具抢占 ADB 服务端口。

## 🛠️ 本地打包

运行 `.\build.ps1` 只会生成 `scrcpy-gui-winui3-portable.zip`，不会自动发布 GitHub Release。如需发布，请显式运行：

```powershell
.\build.ps1 -CreateRelease -ReleaseNotes "更新内容：`n- 修复或新增内容"
```

## 📝 许可证

本项目基于 [MIT License](LICENSE) 开源。
