$ErrorActionPreference = "Stop"

$publishDir = "publish\portable"
$sfxExePath = "ScrcpyGui-SingleFile.exe"
$iconPath = "Assets\AppIcon.ico"
$temp7z = "app_temp.7z"
$tempCfg = "sfx-config.tmp"
$tempSfx = "sfx_module.tmp"

# 1. 查找 7-Zip 及 7zS.sfx
$sevenZipExe = $null
$sfxModule = $null

$candidatePaths = @(
    "C:\Program Files\7-Zip",
    "C:\Program Files (x86)\7-Zip"
)

foreach ($dir in $candidatePaths) {
    if ((Test-Path "$dir\7z.exe") -and (Test-Path "$dir\7zS.sfx")) {
        $sevenZipExe = "$dir\7z.exe"
        $sfxModule = "$dir\7zS.sfx"
        break
    }
}

if (-not $sevenZipExe) {
    $cmd = Get-Command 7z.exe -ErrorAction SilentlyContinue
    if ($cmd) {
        $sevenZipExe = $cmd.Source
        $sfxDir = Split-Path $sevenZipExe
        if (Test-Path "$sfxDir\7zS.sfx") {
            $sfxModule = "$sfxDir\7zS.sfx"
        }
    }
}

if (-not $sevenZipExe -or -not $sfxModule) {
    Write-Host "❌ 未检测到 7-Zip 或 7zS.sfx！请确保已安装官方 7-Zip (包含 7zS.sfx 模块)。" -ForegroundColor Red
    exit 1
}

Write-Host "✅ 找到 7-Zip 工具链: $sevenZipExe" -ForegroundColor Green

# 2. 编译发布便携目录 (若尚未编译)
Write-Host "1/4 正在编译 WinUI 3 Release 二进制文件..." -ForegroundColor Cyan
dotnet publish ScrcpyGui.csproj -c Release -r win-x64 --self-contained false -o $publishDir
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ 编译失败，请检查编译输出！" -ForegroundColor Red
    exit 1
}

if (Test-Path "$publishDir\settings.json") {
    Remove-Item "$publishDir\settings.json" -Force
}

# 3. 使用 7-Zip 进行 LZMA2 高强度压缩
Write-Host "2/4 正在高强度压缩便携目录 (LZMA2 Ultra)..." -ForegroundColor Cyan
if (Test-Path $temp7z) { Remove-Item $temp7z -Force }
$absTemp7z = [System.IO.Path]::GetFullPath($temp7z)
Push-Location $publishDir
try {
    & $sevenZipExe a -t7z -mx9 -mfb=64 -md=32m -ms=on $absTemp7z * | Out-Null
}
finally {
    Pop-Location
}
if ($LASTEXITCODE -ne 0 -or -not (Test-Path $temp7z)) {
    Write-Host "❌ 7-Zip 压缩失败！" -ForegroundColor Red
    exit 1
}

# 4. 准备带有本程序图标的 SFX 外壳
Write-Host "3/4 正在注入应用图标至 SFX 外壳..." -ForegroundColor Cyan
Copy-Item $sfxModule $tempSfx -Force

if (Test-Path $iconPath) {
    $csharpCode = @'
using System;
using System.IO;
using System.Runtime.InteropServices;

public class SfxIconPatcher
{
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr BeginUpdateResource(string pFileName, bool bDeleteExistingResources);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool UpdateResource(IntPtr hUpdate, IntPtr lpType, IntPtr lpName, ushort wLanguage, byte[] lpData, uint cbData);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool EndUpdateResource(IntPtr hUpdate, bool fDiscard);

    private static readonly IntPtr RT_ICON = (IntPtr)3;
    private static readonly IntPtr RT_GROUP_ICON = (IntPtr)14;

    public static bool Patch(string exePath, string icoPath)
    {
        try
        {
            byte[] icoBytes = File.ReadAllBytes(icoPath);
            if (icoBytes.Length < 6) return false;

            ushort type = BitConverter.ToUInt16(icoBytes, 2);
            ushort count = BitConverter.ToUInt16(icoBytes, 4);
            if (type != 1 || count == 0) return false;

            IntPtr hUpdate = BeginUpdateResource(exePath, false);
            if (hUpdate == IntPtr.Zero) return false;

            bool success = true;
            byte[] grpHeader = new byte[6 + count * 14];
            Buffer.BlockCopy(icoBytes, 0, grpHeader, 0, 6);

            for (ushort i = 0; i < count; i++)
            {
                int dirOffset = 6 + i * 16;
                byte width = icoBytes[dirOffset + 0];
                byte height = icoBytes[dirOffset + 1];
                byte colors = icoBytes[dirOffset + 2];
                byte res = icoBytes[dirOffset + 3];
                ushort planes = BitConverter.ToUInt16(icoBytes, dirOffset + 4);
                ushort bpp = BitConverter.ToUInt16(icoBytes, dirOffset + 6);
                uint size = BitConverter.ToUInt32(icoBytes, dirOffset + 8);
                uint offset = BitConverter.ToUInt32(icoBytes, dirOffset + 12);

                byte[] iconData = new byte[size];
                Buffer.BlockCopy(icoBytes, (int)offset, iconData, 0, (int)size);

                ushort iconId = (ushort)(i + 1);
                if (!UpdateResource(hUpdate, RT_ICON, (IntPtr)iconId, 0, iconData, size))
                {
                    success = false;
                    break;
                }

                int grpOffset = 6 + i * 14;
                grpHeader[grpOffset + 0] = width;
                grpHeader[grpOffset + 1] = height;
                grpHeader[grpOffset + 2] = colors;
                grpHeader[grpOffset + 3] = res;
                Buffer.BlockCopy(BitConverter.GetBytes(planes), 0, grpHeader, grpOffset + 4, 2);
                Buffer.BlockCopy(BitConverter.GetBytes(bpp), 0, grpHeader, grpOffset + 6, 2);
                Buffer.BlockCopy(BitConverter.GetBytes(size), 0, grpHeader, grpOffset + 8, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(iconId), 0, grpHeader, grpOffset + 12, 2);
            }

            if (success)
            {
                if (!UpdateResource(hUpdate, RT_GROUP_ICON, (IntPtr)1, 0, grpHeader, (uint)grpHeader.Length))
                {
                    success = false;
                }
            }

            return EndUpdateResource(hUpdate, !success) && success;
        }
        catch
        {
            return false;
        }
    }
}
'@
    try {
        if (-not ([System.Management.Automation.PSTypeName]'SfxIconPatcher').Type) {
            Add-Type -TypeDefinition $csharpCode -Language CSharp
        }
        $iconSuccess = [SfxIconPatcher]::Patch((Resolve-Path $tempSfx).Path, (Resolve-Path $iconPath).Path)
        if ($iconSuccess) {
            Write-Host "   -> 图标注入成功" -ForegroundColor DarkGreen
        }
    } catch {
        Write-Host "   -> 警告：图标注入跳过，将保留默认图标" -ForegroundColor Yellow
    }
}

# 5. 准备 SFX 配置文件并二进制拼接
Write-Host "4/4 正在拼接生成单文件可执行程序..." -ForegroundColor Cyan
$configContent = @'
;!@Install@!UTF-8!
Title="Scrcpy GUI"
BeginPrompt=""
RunProgram="ScrcpyGui.exe"
;!@InstallEnd@!
'@

$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText((Resolve-Path ".").Path + "\" + $tempCfg, $configContent, $utf8NoBom)

if (Test-Path $sfxExePath) {
    Remove-Item $sfxExePath -Force
}

$sfxBytes = [System.IO.File]::ReadAllBytes((Resolve-Path $tempSfx).Path)
$cfgBytes = [System.IO.File]::ReadAllBytes((Resolve-Path $tempCfg).Path)
$arcBytes = [System.IO.File]::ReadAllBytes((Resolve-Path $temp7z).Path)

$outStream = [System.IO.File]::Create((Resolve-Path ".").Path + "\" + $sfxExePath)
$outStream.Write($sfxBytes, 0, $sfxBytes.Length)
$outStream.Write($cfgBytes, 0, $cfgBytes.Length)
$outStream.Write($arcBytes, 0, $arcBytes.Length)
$outStream.Dispose()

# 6. 清理临时中间文件
if (Test-Path $temp7z) { Remove-Item $temp7z -Force }
if (Test-Path $tempCfg) { Remove-Item $tempCfg -Force }
if (Test-Path $tempSfx) { Remove-Item $tempSfx -Force }

$sizeMb = [Math]::Round(((Get-Item $sfxExePath).Length / 1MB), 2)
Write-Host "🎉 7z-SFX 单文件打包成功: $sfxExePath ($sizeMb MB)" -ForegroundColor Green
exit 0
