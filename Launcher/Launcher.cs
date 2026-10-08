using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace ScrcpyLauncher
{
    internal static class Program
    {
        private const string AppFolderName = "ScrcpyGui";
        private const string VersionMarkerFile = ".version";
        private const string TargetExeName = "ScrcpyGui.exe";
        private const string EmbeddedResourceName = "app.zip";

        [STAThread]
        private static void Main(string[] args)
        {
            try
            {
                // 固定缓存目录：%LOCALAPPDATA%\ScrcpyGui\app
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string appDir = Path.Combine(Path.Combine(localAppData, AppFolderName), "app");
                string targetExe = Path.Combine(appDir, TargetExeName);
                string markerPath = Path.Combine(appDir, VersionMarkerFile);

                // 获取内嵌压缩包的构建指纹（基于流长度与内容校验）
                string currentBuildId = GetCurrentBuildId();

                bool needExtract = true;
                if (File.Exists(targetExe) && File.Exists(markerPath))
                {
                    try
                    {
                        string installedBuildId = File.ReadAllText(markerPath).Trim();
                        if (string.Equals(installedBuildId, currentBuildId, StringComparison.OrdinalIgnoreCase))
                        {
                            needExtract = false;
                        }
                    }
                    catch
                    {
                        needExtract = true;
                    }
                }

                if (needExtract)
                {
                    bool mutexCreated = false;
                    using (Mutex mutex = new Mutex(false, "Global\\ScrcpyGui_Extract_Mutex", out mutexCreated))
                    {
                        bool acquired = false;
                        try
                        {
                            acquired = mutex.WaitOne(TimeSpan.FromSeconds(20), false);
                        }
                        catch (AbandonedMutexException)
                        {
                            acquired = true;
                        }

                        // 双重检查避免并发写入
                        if (File.Exists(targetExe) && File.Exists(markerPath))
                        {
                            try
                            {
                                string installedBuildId = File.ReadAllText(markerPath).Trim();
                                if (string.Equals(installedBuildId, currentBuildId, StringComparison.OrdinalIgnoreCase))
                                {
                                    needExtract = false;
                                }
                            }
                            catch { }
                        }

                        if (needExtract)
                        {
                            ExtractPayload(appDir, markerPath, currentBuildId);
                        }

                        if (acquired)
                        {
                            try
                            {
                                mutex.ReleaseMutex();
                            }
                            catch { }
                        }
                    }
                }

                if (!File.Exists(targetExe))
                {
                    MessageBox.Show(
                        string.Format("启动失败：未能在缓存目录找到主程序。\n路径: {0}", targetExe),
                        "Scrcpy GUI 启动器",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }

                // 启动已解压的主程序
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = targetExe;
                psi.WorkingDirectory = appDir;
                psi.UseShellExecute = false;

                if (args != null && args.Length > 0)
                {
                    psi.Arguments = BuildCommandLineArguments(args);
                }

                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format("启动 Scrcpy GUI 时发生异常:\n{0}", ex.Message),
                    "Scrcpy GUI 启动错误",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private static string GetCurrentBuildId()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(EmbeddedResourceName))
            {
                if (stream == null)
                {
                    return "NoPayload_" + assembly.GetName().Version;
                }

                long len = stream.Length;
                byte[] head = new byte[1024];
                int read = stream.Read(head, 0, head.Length);

                int hash = 17;
                for (int i = 0; i < read; i++)
                {
                    hash = hash * 31 + head[i];
                }

                return string.Format("v5.0.1_{0}_{1}", len, hash);
            }
        }

        private static void ExtractPayload(string appDir, string markerPath, string buildId)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(EmbeddedResourceName))
            {
                if (stream == null)
                {
                    throw new FileNotFoundException("未找到嵌入的应用程序包资源 app.zip！");
                }

                if (!Directory.Exists(appDir))
                {
                    Directory.CreateDirectory(appDir);
                }

                using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        string relPath = entry.FullName.Replace('/', '\\');
                        if (relPath.StartsWith("publish\\portable\\", StringComparison.OrdinalIgnoreCase))
                        {
                            relPath = relPath.Substring("publish\\portable\\".Length);
                        }
                        else if (relPath.StartsWith("publish\\", StringComparison.OrdinalIgnoreCase))
                        {
                            relPath = relPath.Substring("publish\\".Length);
                        }
                        else if (relPath.StartsWith("portable\\", StringComparison.OrdinalIgnoreCase))
                        {
                            relPath = relPath.Substring("portable\\".Length);
                        }

                        if (string.IsNullOrEmpty(relPath)) continue;

                        if (string.IsNullOrEmpty(entry.Name) || entry.FullName.EndsWith("/") || entry.FullName.EndsWith("\\"))
                        {
                            string dirPath = Path.Combine(appDir, relPath);
                            if (!Directory.Exists(dirPath))
                            {
                                Directory.CreateDirectory(dirPath);
                            }
                            continue;
                        }

                        string destinationPath = Path.Combine(appDir, relPath);
                        string parentDir = Path.GetDirectoryName(destinationPath);
                        if (!string.IsNullOrEmpty(parentDir) && !Directory.Exists(parentDir))
                        {
                            Directory.CreateDirectory(parentDir);
                        }

                        try
                        {
                            entry.ExtractToFile(destinationPath, true);
                        }
                        catch (IOException ioEx)
                        {
                            // 如果主程序正在运行被锁定
                            if (destinationPath.EndsWith("ScrcpyGui.exe", StringComparison.OrdinalIgnoreCase))
                            {
                                throw new InvalidOperationException("Scrcpy GUI 正在后台运行中，无法更新文件，请先关闭正在运行的程序。", ioEx);
                            }
                            throw;
                        }
                    }
                }

                File.WriteAllText(markerPath, buildId);
            }
        }

        private static string BuildCommandLineArguments(string[] args)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (i > 0)
                {
                    sb.Append(" ");
                }

                if (arg.Contains(" ") || arg.Contains("\""))
                {
                    sb.Append("\"");
                    sb.Append(arg.Replace("\"", "\\\""));
                    sb.Append("\"");
                }
                else
                {
                    sb.Append(arg);
                }
            }
            return sb.ToString();
        }
    }
}
