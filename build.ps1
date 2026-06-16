# 1. 编译打包 (生成单文件便携版，无PDB)
Write-Host "开始本地编译打包..." -ForegroundColor Cyan
dotnet publish -c Release -r win-x64 --self-contained false -o publish\portable
if ($LASTEXITCODE -ne 0) {
    Write-Host "编译失败，请检查错误信息！" -ForegroundColor Red
    exit 1
}

# 2. 压缩成 ZIP
Write-Host "正在压缩 ZIP 文件..." -ForegroundColor Cyan
if (Test-Path "scrcpy-gui-winui3-portable.zip") {
    Remove-Item "scrcpy-gui-winui3-portable.zip" -Force
}
Compress-Archive -Path "publish\portable\*" -DestinationPath "scrcpy-gui-winui3-portable.zip" -Force

# 3. 自动递增版本号 (从最新的 Git 标签获取，若无则使用 v1.0.0)
$latestTag = git describe --tags --abbrev=0 2>$null
if (-not $latestTag) {
    $latestTag = "v1.0.0"
}
# 解析版本号并递增小版本
if ($latestTag -match 'v(\d+)\.(\d+)\.(\d+)') {
    $major = [int]$matches[1]
    $minor = [int]$matches[2]
    $patch = [int]$matches[3]
    $patch += 1
    $newTag = "v$major.$minor.$patch"
} else {
    $newTag = $latestTag + "-update"
}

Write-Host "准备发布到 GitHub Release: $newTag" -ForegroundColor Cyan

# 4. 使用 GitHub CLI 上传 Release
# 清除可能导致冲突的无效环境变量，使用本地 keyring 鉴权
$env:GITHUB_TOKEN = ""

Write-Host "正在推送到 GitHub Releases..." -ForegroundColor Cyan
gh release create $newTag --title "$newTag 自动打包更新" --notes "本地构建生成的免安装便携版 (Portable ZIP)。解压即用。" scrcpy-gui-winui3-portable.zip

if ($LASTEXITCODE -eq 0) {
    Write-Host "🎉 打包发布成功！" -ForegroundColor Green
} else {
    Write-Host "❌ GitHub Release 发布失败！请检查 gh auth status。" -ForegroundColor Red
}
