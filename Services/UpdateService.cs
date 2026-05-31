using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace ScrcpyGui.Services
{
    public class UpdateService
    {
        private readonly PathService _pathService;
        private readonly HttpClient _httpClient;

        public UpdateService(PathService pathService)
        {
            _pathService = pathService;
            _httpClient = new HttpClient();
            // GitHub API requires a User-Agent header
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("scrcpy-gui-winui3");
        }

        public async Task<(string Version, string DownloadUrl)> GetLatestReleaseInfoAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<JsonObject>("https://api.github.com/repos/Genymobile/scrcpy/releases/latest");
                if (response == null) return (string.Empty, string.Empty);

                var tagName = response["tag_name"]?.ToString() ?? string.Empty;
                var assets = response["assets"]?.AsArray();
                
                if (assets == null) return (tagName, string.Empty);

                string downloadUrl = string.Empty;
                foreach (var asset in assets)
                {
                    var name = asset?["name"]?.ToString() ?? string.Empty;
                    if (name.Contains("win64") && name.EndsWith(".zip"))
                    {
                        downloadUrl = asset?["browser_download_url"]?.ToString() ?? string.Empty;
                        break;
                    }
                }

                return (tagName, downloadUrl);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching latest scrcpy release: {ex.Message}");
                // Fallback to a hardcoded version if API fails
                return ("v2.4", "https://github.com/Genymobile/scrcpy/releases/download/v2.4/scrcpy-win64-v2.4.zip");
            }
        }

        public async Task DownloadAndExtractScrcpyAsync(string downloadUrl, Action<double> onProgress, Action<string> onStatusChanged)
        {
            var targetDir = _pathService.DefaultScrcpyDirectory;
            var tempZipPath = Path.Combine(Path.GetTempPath(), "scrcpy-win64-latest.zip");

            onStatusChanged?.Invoke("正在下载 scrcpy 资源包...");
            
            // 1. Download file with progress reporting
            using (var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                using var contentStream = await response.Content.ReadAsStreamAsync();
                using var fileStream = new FileStream(tempZipPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

                var buffer = new byte[8192];
                var totalRead = 0L;
                var bytesRead = 0;

                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                    totalRead += bytesRead;

                    if (totalBytes != -1)
                    {
                        var progress = (double)totalRead / totalBytes * 100;
                        onProgress?.Invoke(progress);
                    }
                }
            }

            onStatusChanged?.Invoke("正在解压运行文件...");
            onProgress?.Invoke(-1); // Indeterminate progress for extraction

            // 2. Extract ZIP
            await Task.Run(() =>
            {
                if (Directory.Exists(targetDir))
                {
                    Directory.Delete(targetDir, true);
                }
                Directory.CreateDirectory(targetDir);

                using var archive = ZipFile.OpenRead(tempZipPath);
                
                // Usually the ZIP contains a folder like "scrcpy-win64-vX.Y"
                // We want to extract files inside that subfolder directly into our target directory
                string rootFolderInZip = string.Empty;
                foreach (var entry in archive.Entries)
                {
                    if (entry.FullName.Contains("/") && rootFolderInZip == string.Empty)
                    {
                        rootFolderInZip = entry.FullName.Split('/')[0] + "/";
                        break;
                    }
                }

                foreach (var entry in archive.Entries)
                {
                    if (entry.FullName.EndsWith("/")) continue; // Skip directory entries

                    string relativePath = entry.FullName;
                    if (!string.IsNullOrEmpty(rootFolderInZip) && relativePath.StartsWith(rootFolderInZip))
                    {
                        relativePath = relativePath.Substring(rootFolderInZip.Length);
                    }

                    var destinationPath = Path.Combine(targetDir, relativePath);
                    var destDir = Path.GetDirectoryName(destinationPath);
                    if (destDir != null && !Directory.Exists(destDir))
                    {
                        Directory.CreateDirectory(destDir);
                    }

                    entry.ExtractToFile(destinationPath, true);
                }
            });

            // 3. Clean up temp file
            if (File.Exists(tempZipPath))
            {
                File.Delete(tempZipPath);
            }

            onStatusChanged?.Invoke("安装完成");
            onProgress?.Invoke(100);
        }
    }
}
