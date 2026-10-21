using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using TinyWinBuilder.Core.Imaging;
using TinyWinBuilder.Core.Unattended;

namespace TinyWinBuilder.App.Views;

public partial class MainWindow : Window
{
    private readonly ImageManager _imageMgr = new();
    private string? _mountedIsoPath;
    private DriveInfo? _isoDrive;

    public List<AppItem> Apps { get; set; } = new()
    {
        new() { Id = "Microsoft.GamingApp", Name = "Xbox 应用" },
        new() { Id = "Microsoft.BingNews", Name = "Bing 新闻" },
        new() { Id = "Microsoft.BingWeather", Name = "Bing 天气" },
        new() { Id = "Microsoft.BingSearch", Name = "Bing 搜索" },
        new() { Id = "microsoft.windowscommunicationsapps", Name = "邮件和日历" },
        new() { Id = "Microsoft.People", Name = "人脉" },
        new() { Id = "Microsoft.SkypeApp", Name = "Skype" },
        new() { Id = "Microsoft.WindowsFeedbackHub", Name = "反馈中心" },
        new() { Id = "Microsoft.GetHelp", Name = "获取帮助" },
        new() { Id = "Microsoft.Getstarted", Name = "入门应用" },
        new() { Id = "Microsoft.YourPhone", Name = "手机连接" },
        new() { Id = "Microsoft.MicrosoftSolitaireCollection", Name = "纸牌游戏" },
    };

    public List<FeatureItem> Features { get; set; } = new()
    {
        new() { Id = "Internet-Explorer-Optional-amd64", Name = "Internet Explorer 11" },
        new() { Id = "WindowsMediaPlayer", Name = "Windows Media Player" },
        new() { Id = "Windows-WordPad", Name = "写字板" },
        new() { Id = "Microsoft-Windows-StepsRecorder", Name = "步骤记录器" },
    };

    public List<TweakItem> Tweaks { get; set; } = new()
    {
        new() { Name = "禁用 Windows Defender", Desc = "完全禁用实时保护（不推荐）", IsChecked = false },
        new() { Name = "禁用 OneDrive", Desc = "卸载并阻止同步", IsChecked = true },
        new() { Name = "禁用 Cortana", Desc = "禁用语音助手", IsChecked = true },
        new() { Name = "禁用遥测", Desc = "设置遥测级别为最低", IsChecked = true },
        new() { Name = "禁用广告推荐", Desc = "禁用开始菜单/锁屏广告", IsChecked = true },
        new() { Name = "禁用 Xbox Game Bar", Desc = "禁用游戏栏和录制", IsChecked = true },
    };

    public MainWindow()
    {
        InitializeComponent();
        AppsList.ItemsSource = Apps;
        FeaturesList.ItemsSource = Features;
        TweaksList.ItemsSource = Tweaks;
        Log("TinyWin Builder 已启动。请选择左侧功能开始。");
        Log("提示：需要管理员权限才能挂载和修改镜像。");
    }

    private void NavList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (NavList.SelectedItem is not ListBoxItem) return;
        
        if (ImagePanel == null || AppsPanel == null || FeaturesPanel == null || 
            TweaksPanel == null || UnattendedPanel == null || ApplyPanel == null)
            return;

        var panels = new[] { ImagePanel, AppsPanel, FeaturesPanel, TweaksPanel, UnattendedPanel, ApplyPanel };
        foreach (var p in panels) p.Visibility = Visibility.Collapsed;

        var idx = NavList.SelectedIndex;
        if (idx >= 0 && idx < panels.Length)
            panels[idx].Visibility = Visibility.Visible;
    }

    private string? _localWimPath;

    private void BrowseWim_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Filter = "镜像文件|*.iso;*.wim|ISO 镜像|*.iso|WIM 镜像|*.wim|所有文件|*.*" };
        if (dlg.ShowDialog() == true)
        {
            WimPathBox.Text = dlg.FileName;
            Log($"已选择镜像: {dlg.FileName}");
            
            // 自动加载镜像版本列表
            _ = LoadImageVersionsAsync(dlg.FileName);
        }
    }

    private async Task LoadImageVersionsAsync(string imagePath)
    {
        try
        {
            MountBtn.IsEnabled = false;
            var ext = Path.GetExtension(imagePath).ToLower();

            if (ext == ".iso")
            {
                Log("正在挂载 ISO 镜像...");
                await MountIsoAsync(imagePath);
                
                if (_isoDrive == null)
                {
                    throw new InvalidOperationException("ISO 挂载失败，未获取到盘符");
                }
                
                var isoWimPath = Path.Combine(_isoDrive.RootDirectory.FullName, "sources", "install.wim");
                if (!File.Exists(isoWimPath))
                {
                    isoWimPath = Path.Combine(_isoDrive.RootDirectory.FullName, "sources", "install.esd");
                }
                
                if (!File.Exists(isoWimPath))
                {
                    throw new FileNotFoundException($"在 ISO 中未找到 install.wim 或 install.esd");
                }
                
                Log($"在 ISO 中找到镜像文件: {isoWimPath}");
                
                // 把 WIM 复制到本地磁盘（ISO 是只读的）
                Log("正在将 WIM 复制到本地磁盘...");
                // 使用桌面目录，避免 Temp 目录权限问题
                var workDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "TinyWinBuilder_Work");
                if (!Directory.Exists(workDir))
                {
                    Directory.CreateDirectory(workDir);
                }
                _localWimPath = Path.Combine(workDir, "install.wim");
                
                // 如果文件已存在，先尝试删除
                if (File.Exists(_localWimPath))
                {
                    try
                    {
                        File.Delete(_localWimPath);
                    }
                    catch
                    {
                        // 如果删除失败，用新文件名
                        _localWimPath = Path.Combine(workDir, $"install_{DateTime.Now:yyyyMMdd_HHmmss}.wim");
                    }
                }
                
                var fileInfo = new FileInfo(isoWimPath);
                Log($"文件大小: {fileInfo.Length / 1024 / 1024:.0} MB，请稍候...");
                
                File.Copy(isoWimPath, _localWimPath);
                Log("WIM 复制完成！");
                
                _originalWimPath = _localWimPath;
            }
            else if (ext == ".wim")
            {
                _localWimPath = imagePath;
                _originalWimPath = imagePath;
            }

            Log("正在读取镜像版本列表...");
            var images = await _imageMgr.GetImageIndexesAsync(_localWimPath);
            ImageIndexCombo.ItemsSource = images;
            ImageIndexCombo.DisplayMemberPath = "Name";
            ImageIndexCombo.SelectedIndex = 0;
            
            Log($"找到 {images.Count} 个版本，请在下拉框中选择后点击「挂载镜像」");
            MountBtn.IsEnabled = true;
        }
        catch (Exception ex)
        {
            Log($"加载镜像失败: {ex.Message}");
            MessageBox.Show($"加载镜像失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            MountBtn.IsEnabled = true;
        }
    }

    private async void Mount_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_localWimPath))
        {
            MessageBox.Show("请先选择镜像文件！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            MountBtn.IsEnabled = false;
            
            // 获取用户选择的版本
            var selectedImage = ImageIndexCombo.SelectedItem as ImageInfo;
            var imageIndex = selectedImage?.Index ?? 1;
            var imageName = selectedImage?.Name ?? "未知版本";
            
            Log($"正在挂载 WIM 镜像: {imageName} (Index: {imageIndex})...");
            
            var workDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "TinyWinBuilder_Work");
            var mountDir = Path.Combine(workDir, "Mount");
            if (Directory.Exists(mountDir))
            {
                try { Directory.Delete(mountDir, true); } catch { }
            }
            Directory.CreateDirectory(mountDir);
            
            await _imageMgr.MountAsync(_localWimPath, imageIndex, mountDir);

            ApplyBtn.IsEnabled = true;
            Log("镜像挂载成功！");
            Log("现在可以在左侧选择要精简的应用和功能了。");
        }
        catch (Exception ex)
        {
            Log($"挂载失败: {ex.Message}");
            MessageBox.Show($"挂载失败: {ex.Message}\n\n请确保以管理员身份运行程序。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            MountBtn.IsEnabled = true;
        }
    }

    private async Task MountIsoAsync(string isoPath)
    {
        // 先用 PowerShell 挂载 ISO，然后获取盘符
        var script = @"
$isoPath = '" + isoPath + @"'
try {
    $result = Mount-DiskImage -ImagePath $isoPath -PassThru
    Start-Sleep -Seconds 2
    $driveLetter = ($result | Get-Volume).DriveLetter
    Write-Output $driveLetter
} catch {
    Write-Output 'ERROR:' + $_.Exception.Message
}
";

        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -Command \"{script}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        
        var process = Process.Start(psi)!;
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        
        output = output.Trim();
        Log($"PowerShell 输出: {output}");
        
        if (output.StartsWith("ERROR:"))
        {
            throw new InvalidOperationException(output.Substring(6));
        }
        
        var driveLetter = output.Trim();
        if (string.IsNullOrEmpty(driveLetter) || driveLetter.Length != 1)
        {
            throw new InvalidOperationException("无法获取 ISO 盘符，可能挂载失败");
        }
        
        _isoDrive = new DriveInfo(driveLetter + ":");
        _mountedIsoPath = isoPath;
        Log($"ISO 已挂载到盘符: {driveLetter}:");
    }

    private async void Unmount_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            UnmountBtn.IsEnabled = false;
            Log("正在清理...");
            
            // 1. 卸载 WIM 镜像（如果还挂载着）
            if (_imageMgr.IsMounted)
            {
                Log("正在卸载 WIM 镜像...");
                await _imageMgr.UnmountAsync(true);
                Log("WIM 镜像已卸载。");
            }
            else
            {
                Log("WIM 镜像已卸载。");
            }
            
            // 2. 卸载 ISO
            if (_isoDrive != null && !string.IsNullOrEmpty(_mountedIsoPath))
            {
                Log("正在卸载 ISO...");
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -Command \"Dismount-DiskImage -ImagePath '{_mountedIsoPath}'\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                Process.Start(psi)!.WaitForExit();
                Log("ISO 已卸载。");
                _isoDrive = null;
                _mountedIsoPath = null;
            }
            
            // 3. 清理临时文件
            Log("正在清理临时文件...");
            try
            {
                var workDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "TinyWinBuilder_Work");
                if (Directory.Exists(workDir))
                {
                    Directory.Delete(workDir, true);
                    Log("临时文件已清理。");
                }
            }
            catch (Exception ex)
            {
                Log($"清理临时文件失败（可手动删除）: {ex.Message}");
            }

            // 4. 重置所有按钮状态
            MountBtn.IsEnabled = true;
            ApplyBtn.IsEnabled = false;
            CommitBtn.IsEnabled = false;
            ExportWimBtn.IsEnabled = false;
            ExportIsoBtn.IsEnabled = false;
            UnmountBtn.IsEnabled = false;
            
            // 清空镜像选择
            WimPathBox.Text = "";
            ImageIndexCombo.ItemsSource = null;
            _localWimPath = null;
            _originalWimPath = null;

            Log("✅ 全部清理完成！可以选择新的镜像开始下一次操作。");
        }
        catch (Exception ex)
        {
            Log($"清理失败: {ex.Message}");
            MessageBox.Show($"清理失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            UnmountBtn.IsEnabled = true;
        }
    }

    private async void Commit_Click(object sender, RoutedEventArgs e)
    {
        if (!_imageMgr.IsMounted)
        {
            MessageBox.Show("请先挂载镜像并应用更改！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            Log("正在提交更改（卸载并保存）...");
            await _imageMgr.UnmountAsync(true);
            Log("更改已提交到 WIM 镜像文件！");
            Log("现在可以导出精简后的镜像了。");
            
            // 启用导出按钮和卸载按钮
            ExportWimBtn.IsEnabled = true;
            ExportIsoBtn.IsEnabled = true;
            UnmountBtn.IsEnabled = true;
        }
        catch (Exception ex)
        {
            Log($"提交失败: {ex.Message}");
            MessageBox.Show($"提交失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void Apply_Click(object sender, RoutedEventArgs e)
    {
        if (!_imageMgr.IsMounted)
        {
            MessageBox.Show("请先挂载镜像！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            ApplyPanel.IsEnabled = false;
            Log("开始应用更改...");

            // 移除勾选的预装应用
            var appsToRemove = Apps.Where(a => a.IsChecked).ToList();
            Log($"将移除 {appsToRemove.Count} 个预装应用...");
            foreach (var app in appsToRemove)
            {
                Log($"  移除: {app.Name}");
                await _imageMgr.RemoveProvisionedAppAsync(app.Id);
            }

            // 禁用勾选的系统功能
            var featuresToDisable = Features.Where(f => f.IsChecked).ToList();
            Log($"将禁用 {featuresToDisable.Count} 个系统功能...");
            foreach (var feature in featuresToDisable)
            {
                Log($"  禁用: {feature.Name}");
                await _imageMgr.DisableFeatureAsync(feature.Id);
            }

            // 清理组件存储
            Log("正在清理组件存储...");
            await _imageMgr.CleanupImageAsync();
            Log("组件清理完成。");

            Log("✅ 所有更改已应用！");
            Log("请点击「步骤 2：提交更改」按钮保存修改。");
            MessageBox.Show("所有更改已应用！\n\n请点击「步骤 2：提交更改」按钮保存修改。", 
                "完成", MessageBoxButton.OK, MessageBoxImage.Information);
            
            // 启用提交按钮
            CommitBtn.IsEnabled = true;
        }
        catch (Exception ex)
        {
            Log($"应用更改失败: {ex.Message}");
            MessageBox.Show($"应用更改失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            ApplyPanel.IsEnabled = true;
        }
    }

    private string? _originalWimPath;

    private async void ExportWim_Click(object sender, RoutedEventArgs e)
    {
        if (_imageMgr.IsMounted)
        {
            MessageBox.Show("请先卸载镜像并提交更改！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (string.IsNullOrEmpty(_originalWimPath))
        {
            MessageBox.Show("没有找到原始镜像路径。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var dlg = new SaveFileDialog { Filter = "WIM 镜像|*.wim", FileName = "TinyWin10.wim" };
            if (dlg.ShowDialog() != true) return;

            Log($"正在导出精简后的 WIM 到: {dlg.FileName}");
            ApplyPanel.IsEnabled = false;
            ExportProgressBar.Visibility = Visibility.Visible;
            ExportProgressBar.Value = 0;

            // 用 DISM 导出 WIM
            var psi = new ProcessStartInfo
            {
                FileName = "dism.exe",
                Arguments = $"/Export-Image /SourceImageFile:\"{_originalWimPath}\" /SourceIndex:1 /DestinationImageFile:\"{dlg.FileName}\" /Compress:max",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true
            };

            var process = Process.Start(psi)!;
            
            // 实时读取输出，更新进度条
            var outputBuilder = new System.Text.StringBuilder();
            while (!process.StandardOutput.EndOfStream)
            {
                var line = await process.StandardOutput.ReadLineAsync();
                outputBuilder.AppendLine(line);
                
                // 解析百分比，比如 "[==========================10.0%==========================]"
                var match = System.Text.RegularExpressions.Regex.Match(line, @"(\d+(?:\.\d+)?)%");
                if (match.Success && double.TryParse(match.Groups[1].Value, out var percent))
                {
                    ExportProgressBar.Value = percent;
                    Log($"导出进度: {percent:F1}%");
                }
            }
            
            await process.WaitForExitAsync();
            var output = outputBuilder.ToString();

            if (process.ExitCode == 0)
            {
                // 验证文件是否真的生成了
                if (File.Exists(dlg.FileName))
                {
                    var fileInfo = new FileInfo(dlg.FileName);
                    var sizeMB = fileInfo.Length / 1024 / 1024;
                    Log($"✅ WIM 导出成功！文件大小: {sizeMB:F1} MB");
                    MessageBox.Show($"WIM 导出成功！\n\n文件: {dlg.FileName}\n大小: {sizeMB:F1} MB", 
                        "完成", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    Log("❌ 导出命令返回成功，但文件不存在！");
                    MessageBox.Show("导出失败：文件未生成！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                Log($"导出失败，退出码: {process.ExitCode}");
                Log($"错误输出: {output}");
                MessageBox.Show($"导出失败，退出码: {process.ExitCode}\n\n请查看日志了解详情。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            Log($"导出失败: {ex.Message}");
            MessageBox.Show($"导出失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            ApplyPanel.IsEnabled = true;
            ExportProgressBar.Visibility = Visibility.Collapsed;
        }
    }

    private async void ExportIso_Click(object sender, RoutedEventArgs e)
    {
        if (_imageMgr.IsMounted)
        {
            MessageBox.Show("请先卸载镜像并提交更改！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (string.IsNullOrEmpty(_originalWimPath))
        {
            MessageBox.Show("没有找到原始镜像路径。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var dlg = new SaveFileDialog { Filter = "ISO 镜像|*.iso", FileName = "TinyWin10.iso" };
            if (dlg.ShowDialog() != true) return;

            Log("正在准备导出 ISO...");
            Log("步骤 1: 导出精简后的 WIM...");
            
            // 先导出 WIM 到临时目录
            var tempWim = Path.Combine(Path.GetTempPath(), "TinyWin_export.wim");
            
            var psi = new ProcessStartInfo
            {
                FileName = "dism.exe",
                Arguments = $"/Export-Image /SourceImageFile:\"{_originalWimPath}\" /SourceIndex:1 /DestinationImageFile:\"{tempWim}\" /Compress:max",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true
            };

            var process = Process.Start(psi)!;
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                Log("WIM 导出失败");
                return;
            }

            Log("步骤 2: 复制原始 ISO 文件...");
            
            // 从原始 ISO 复制文件到临时目录
            var isoExtractDir = Path.Combine(Path.GetTempPath(), "TinyWin_ISO");
            if (Directory.Exists(isoExtractDir))
            {
                Directory.Delete(isoExtractDir, true);
            }
            Directory.CreateDirectory(isoExtractDir);

            // 挂载原始 ISO
            Log("正在挂载原始 ISO...");
            var mountScript = $"Mount-DiskImage -ImagePath '{WimPathBox.Text}' | Get-Volume | Select-Object -ExpandProperty DriveLetter";
            var mountPsi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -Command \"{mountScript}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true
            };
            var mountProcess = Process.Start(mountPsi)!;
            var driveLetter = (await mountProcess.StandardOutput.ReadToEndAsync()).Trim();
            await mountProcess.WaitForExitAsync();

            if (string.IsNullOrEmpty(driveLetter))
            {
                Log("无法挂载原始 ISO");
                return;
            }

            Log($"ISO 已挂载到 {driveLetter}:");

            // 复制文件
            Log("正在复制 ISO 文件...");
            var sourceDir = $"{driveLetter}:\\";
            CopyDirectory(sourceDir, isoExtractDir);

            // 替换 install.wim
            Log("正在替换 install.wim...");
            var targetWim = Path.Combine(isoExtractDir, "sources", "install.wim");
            if (File.Exists(targetWim))
            {
                File.Delete(targetWim);
            }
            File.Copy(tempWim, targetWim);

            // 卸载 ISO
            Log("正在卸载原始 ISO...");
            var unmountScript = $"Dismount-DiskImage -ImagePath '{WimPathBox.Text}'";
            var unmountPsi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -Command \"{unmountScript}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            Process.Start(unmountPsi)!.WaitForExit();

            // 生成新 ISO（使用 oscdimg，如果没有就提示）
            Log("正在生成新 ISO...");
            Log("（注意：需要 Windows ADK 的 oscdimg 工具）");
            
            // 检查 oscdimg 是否存在
            var oscdimgPaths = new[]
            {
                @"C:\Program Files (x86)\Windows Kits\10\Assessment and Deployment Kit\Deployment Tools\amd64\Oscdimg\oscdimg.exe",
                @"C:\Program Files\Windows Kits\10\Assessment and Deployment Kit\Deployment Tools\amd64\Oscdimg\oscdimg.exe"
            };

            var oscdimgPath = oscdimgPaths.FirstOrDefault(p => File.Exists(p));
            
            if (oscdimgPath == null)
            {
                Log("❌ 未找到 oscdimg 工具");
                Log("请安装 Windows ADK，或者手动用 UltraISO 等工具制作 ISO");
                Log($"ISO 文件已准备好: {isoExtractDir}");
                MessageBox.Show("未找到 oscdimg 工具。\n\nISO 文件已准备好到临时目录，你可以手动用 UltraISO 等工具制作 ISO。", 
                    "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var etfsboot = Path.Combine(isoExtractDir, "boot", "etfsboot.com");
            var isoLabel = "TinyWin10";
            
            var isoPsi = new ProcessStartInfo
            {
                FileName = oscdimgPath,
                Arguments = $"-m -o -u2 -udfver102 -b\"{etfsboot}\" \"{isoExtractDir}\" \"{dlg.FileName}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true
            };

            var isoProcess = Process.Start(isoPsi)!;
            await isoProcess.WaitForExitAsync();

            if (isoProcess.ExitCode == 0)
            {
                Log("✅ ISO 导出成功！");
                MessageBox.Show("ISO 导出成功！\n\n精简后的 ISO 已保存到你选择的位置。", 
                    "完成", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                Log("ISO 生成失败");
            }
        }
        catch (Exception ex)
        {
            Log($"导出 ISO 失败: {ex.Message}");
            MessageBox.Show($"导出 ISO 失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);
        
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var destFile = Path.Combine(destDir, Path.GetFileName(file));
            File.Copy(file, destFile, true);
        }
        
        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            var destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
            CopyDirectory(dir, destSubDir);
        }
    }

    private void PreviewUnattended_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var settings = new UnattendedSettings
            {
                Username = UaUsername.Text,
                AutoLoginCount = int.TryParse(UaAutoLogin.Text, out var count) ? count : 1
            };
            var xml = UnattendedGenerator.Generate(settings);
            Log("自动部署 XML 预览...");
            MessageBox.Show(xml, "autounattend.xml 预览", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            Log($"预览失败: {ex.Message}");
        }
    }

    private void SaveUnattended_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var settings = new UnattendedSettings
            {
                Username = UaUsername.Text,
                AutoLoginCount = int.TryParse(UaAutoLogin.Text, out var count) ? count : 1
            };
            var xml = UnattendedGenerator.Generate(settings);
            
            var dlg = new SaveFileDialog { Filter = "XML 文件|*.xml", FileName = "autounattend.xml" };
            if (dlg.ShowDialog() == true)
            {
                File.WriteAllText(dlg.FileName, xml);
                Log($"自动部署文件已保存: {dlg.FileName}");
                MessageBox.Show("自动部署文件已保存！\n\n将此文件放在 ISO 根目录下即可实现无人值守安装。", 
                    "完成", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            Log($"保存失败: {ex.Message}");
            MessageBox.Show($"保存失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Log(string msg)
    {
        LogBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}\n");
        LogBox.ScrollToEnd();
    }
}

public class AppItem
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public bool IsChecked { get; set; }
}

public class FeatureItem
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public bool IsChecked { get; set; }
}

public class TweakItem
{
    public string Name { get; set; } = "";
    public string Desc { get; set; } = "";
    public bool IsChecked { get; set; }
}
