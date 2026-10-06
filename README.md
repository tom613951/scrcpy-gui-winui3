# Scrcpy GUI (WinUI 3)

基于 C# 与 WinUI 3 开发的 scrcpy 图形化控制面板。

## 功能特性

- **适配 scrcpy v5.0+ 硬件解码**：支持 GPU 硬件加速解码（Auto / D3D11VA / 禁用软解），大幅降低 CPU 负载与功耗；支持视频缓冲延迟调节（Video Buffer）。
- **WinUI 3 界面**：采用 Windows 11 Fluent Design 卡片布局。
- **多模态 AI 辅助控制**：支持接入具备视觉能力的多模态大模型，通过自然语言指令识别屏幕元素并执行点击、滑动与文本输入。
- **自定义组件路径与自动探测**：程序体积轻量，支持优先自动探测同级目录与系统 PATH，或在设置中手动指定 scrcpy 与 adb 路径。
- **设备与无线连接**：支持 USB 与无线 ADB 连接管理，提供服务重启、退出时关闭后台 ADB 以及 Android 11+ 无线配对。
- **投屏模式**：支持屏幕镜像、相机画面调用（Camera，支持方向旋转与变焦）与虚拟副屏（Desktop）。
- **外设输入直通**：支持 UHID 模拟物理键盘与鼠标直通。
- **拖拽文件传输**：支持拖入 APK 自动覆盖安装（-r），拖入常规文件推送至设备存储目录。
- **参数配置与终端日志**：提供比特率、分辨率与帧率调节，内置日志控制台与 ADB 终端交互面板。

## 快速开始

1. **运行环境**：需要安装 [.NET 9 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)。如遇运行库错误，请一并安装 Microsoft Windows App Runtime。
2. **下载运行**：从 [Releases 页面](https://github.com/tom613951/scrcpy-gui-winui3/releases) 下载 scrcpy-gui-winui3-portable.zip，解压后运行 ScrcpyGui.exe。
3. **配置路径**：首次启动进入“系统设置”，指定本地 scrcpy 所在目录（支持 scrcpy v5.0+）；如 adb.exe 位于独立路径，可单独指定。

## 使用提示

- 程序退出时会自动终止指定配置路径下的 ADB 进程，避免后台残留。
- 设备列表刷新异常时，可点击“重启 ADB 服务”重新初始化连接。
- 多工具共存时建议在设置中显式指定 ADB 路径，避免端口冲突。

## 本地构建

```powershell
dotnet publish ScrcpyGui.csproj -c Release -r win-x64 --self-contained false -o publish\portable
```

构建完成后将在 `publish\portable` 目录下生成可执行文件。

## 开源协议

本项目采用 [MIT](LICENSE) 协议开源。