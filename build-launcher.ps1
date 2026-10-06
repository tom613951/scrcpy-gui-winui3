$ErrorActionPreference = "Stop"

$publishDir = "publish\portable"
$singleFileExe = "ScrcpyGui.exe"
$iconPath = "Assets\AppIcon.ico"
$launcherSource = "Launcher\Launcher.cs"
$tempZip = "app_payload.zip"

# 1. 查找 7-Zip (用于高强度生成标准 ZIP)
$sevenZipExe = $null
$candidatePaths = @(
    "C:\Program Files\7-Zip\7z.exe",
    "C:\Program Files (x86)\7-Zip\7z.exe"
)

foreach ($path in $candidatePaths) {
    if (Test-Path $path) {
        $sevenZipExe = $path
        break
    }
}

if (-not $sevenZipExe) {
    $cmd = Get-Command 7z.exe -ErrorAction SilentlyContinue
    if ($cmd) { $sevenZipExe = $cmd.Source }
}

# 2. 查找 Windows 自带的 C# 编译器 csc.exe
$cscExe = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path $cscExe)) {
    $cscExe = "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
}

if (-not (Test-Path $cscExe)) {
    Write-Host "❌ 未能在系统中找到 .NET Framework csc.exe 编译器！" -ForegroundColor Red
    exit 1
}

Write-Host "✅ 工具链就绪:" -ForegroundColor Green
Write-Host "   - 编译器: $cscExe" -ForegroundColor Gray
Write-Host "   - 压缩器: $(if ($sevenZipExe) { $sevenZipExe } else { '内置 Compress-Archive' })" -ForegroundColor Gray

# 3. 编译发布 WinUI 3 Release (关闭 R2R 避免冗余膨胀，保留即时编译高性能)
Write-Host "1/4 正在编译 WinUI 3 Release 二进制文件 (体积优化)..." -ForegroundColor Cyan
dotnet build ScrcpyGui.csproj -c Release -r win-x64 -p:PublishReadyToRun=false
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ 编译失败！" -ForegroundColor Red
    exit 1
}

if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
dotnet publish ScrcpyGui.csproj -c Release -r win-x64 --self-contained false -p:PublishReadyToRun=false -o $publishDir --no-build
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ 发布失败！" -ForegroundColor Red
    exit 1
}

if (Test-Path "$publishDir\settings.json") {
    Remove-Item "$publishDir\settings.json" -Force
}

# 裁剪冗余的非中英多国语言包 (精简 ~15MB 原始体积与几百个文件)
Get-ChildItem $publishDir -Directory | Where-Object { $_.Name -notmatch "^(zh|en)" } | Remove-Item -Recurse -Force

# 4. 打包便携目录为标准 ZIP 资源
Write-Host "2/4 正在高压缩便携目录 (极限 Deflate)..." -ForegroundColor Cyan
if (Test-Path $tempZip) { Remove-Item $tempZip -Force }

$absZip = [System.IO.Path]::GetFullPath($tempZip)
Push-Location $publishDir
try {
    if ($sevenZipExe) {
        & $sevenZipExe a -tzip -mx9 -mfb=258 -mpass=15 $absZip "*" | Out-Null
    } else {
        Compress-Archive -Path "*" -DestinationPath $absZip -CompressionLevel Optimal
    }
} finally {
    Pop-Location
}

if (-not (Test-Path $tempZip)) {
    Write-Host "❌ 压缩失败！" -ForegroundColor Red
    exit 1
}

# 5. 使用 csc.exe 编译固定缓存目录启动器并内嵌 ZIP
Write-Host "3/4 正在编译无黑窗原生启动器并内嵌资源..." -ForegroundColor Cyan

$absIcon = [System.IO.Path]::GetFullPath($iconPath)
$absSource = [System.IO.Path]::GetFullPath($launcherSource)
$absZip = [System.IO.Path]::GetFullPath($tempZip)
$absOut = [System.IO.Path]::GetFullPath($singleFileExe)

$compileArgs = @(
    "/target:winexe",
    "/optimize+",
    "/win32icon:$absIcon",
    "/r:System.dll,System.Core.dll,System.IO.Compression.dll,System.IO.Compression.FileSystem.dll,System.Windows.Forms.dll",
    "/resource:$absZip,app.zip",
    "/out:$absOut",
    $absSource
)

& $cscExe $compileArgs | Out-Null
if ($LASTEXITCODE -ne 0 -or -not (Test-Path $singleFileExe)) {
    Write-Host "❌ 启动器编译失败！" -ForegroundColor Red
    & $cscExe $compileArgs
    exit 1
}

# 6. 保存便携版 ZIP 并清理临时文件
Write-Host "4/4 正在输出便携版 ZIP 与清理临时文件..." -ForegroundColor Cyan
$portableZip = "scrcpy-gui-winui3-portable.zip"
Copy-Item $tempZip $portableZip -Force
if (Test-Path $tempZip) { Remove-Item $tempZip -Force }

$sizeMb = [Math]::Round(((Get-Item $singleFileExe).Length / 1MB), 2)
$zipSizeMb = [Math]::Round(((Get-Item $portableZip).Length / 1MB), 2)
Write-Host "🎉 构建完成:" -ForegroundColor Green
Write-Host "   - 单文件版: $singleFileExe ($sizeMb MB)" -ForegroundColor Green
Write-Host "   - 便携包版: $portableZip ($zipSizeMb MB)" -ForegroundColor Green
Write-Host "   特性: 首次极速解压至 %LOCALAPPDATA%\ScrcpyGui\app，之后次次秒开，无任何解压动画与报错！" -ForegroundColor DarkCyan
exit 0
