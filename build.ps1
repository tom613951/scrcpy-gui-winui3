$ErrorActionPreference = "Stop"
$publishDir = "publish\portable"
$zipPath = "scrcpy-gui-winui3-portable.zip"

# 1. 编译打包 (生成便携版，无PDB)
Write-Host "开始本地编译打包..." -ForegroundColor Cyan
dotnet publish ScrcpyGui.csproj -c Release -r win-x64 --self-contained false -o $publishDir
if ($LASTEXITCODE -ne 0) {
    Write-Host "编译失败，请检查错误信息！" -ForegroundColor Red
    exit 1
}

# 2. 压缩成 ZIP
Write-Host "正在清理敏感配置与缓存..." -ForegroundColor Cyan
if (Test-Path "$publishDir\settings.json") {
    Remove-Item "$publishDir\settings.json" -Force
}

Write-Host "正在压缩 ZIP 文件..." -ForegroundColor Cyan
if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}
Compress-Archive -Path "$publishDir\*" -DestinationPath $zipPath -Force

Write-Host "🎉 打包完成: $zipPath" -ForegroundColor Green
exit 0
