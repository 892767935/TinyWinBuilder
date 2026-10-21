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

    private void BrowseWim_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Filter = "镜像文件|*.iso;*.wim|ISO 镜像|*.iso|WIM 镜像|*.wim|所有文件|*.*" };
        if (dlg.ShowDialog() == true)
        {
            WimPathBox.Text = dlg.FileName;
            Log($"已选择镜像: {dlg.FileName}");
        }
    }

    private async void Mount_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(WimPathBox.Text))
        {
            MessageBox.Show("请先选择镜像文件！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            MountBtn.IsEnabled = false;
            var ext = Path.GetExtension(WimPathBox.Text).ToLower();

            if (ext == ".iso")
            {
                Log("正在挂载 ISO 镜像...");
                await MountIsoAsync(WimPathBox.Text);
                
                if (_isoDrive == null)
                {
                    throw new InvalidOperationException("ISO 挂载失败，未获取到盘符");
                }
                
                var wimPath = Path.Combine(_isoDrive.RootDirectory.FullName, "sources", "install.wim");
                if (!File.Exists(wimPath))
                {
                    wimPath = Path.Combine(_isoDrive.RootDirectory.FullName, "sources", "install.esd");
                }
                
                if (!File.Exists(wimPath))
                {
                    throw new FileNotFoundException($"在 ISO 中未找到 install.wim 或 install.esd");
                }
                
                Log($"找到镜像文件: {wimPath}");
                Log("正在读取镜像版本列表...");
                
                var images = await _imageMgr.GetImageIndexesAsync(wimPath);
                ImageIndexCombo.ItemsSource = images;
                ImageIndexCombo.DisplayMemberPath = "Name";
                ImageIndexCombo.SelectedIndex = 0;
                
                Log($"找到 {images.Count} 个版本");
                Log("正在挂载 WIM 镜像...");
                
                var mountDir = Path.Combine(Path.GetTempPath(), "TinyWinBuilder_Mount");
                await _imageMgr.MountAsync(wimPath, 1, mountDir);
            }
            else if (ext == ".wim")
            {
                Log("正在挂载 WIM 镜像...");
                var images = await _imageMgr.GetImageIndexesAsync(WimPathBox.Text);
                ImageIndexCombo.ItemsSource = images;
                ImageIndexCombo.DisplayMemberPath = "Name";
                ImageIndexCombo.SelectedIndex = 0;
                
                var mountDir = Path.Combine(Path.GetTempPath(), "TinyWinBuilder_Mount");
                await _imageMgr.MountAsync(WimPathBox.Text, 1, mountDir);
            }

            UnmountBtn.IsEnabled = true;
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
            Log("正在卸载镜像并提交更改...");
            
            if (_imageMgr.IsMounted)
            {
                await _imageMgr.UnmountAsync(true);
                Log("WIM 镜像已卸载，更改已提交。");
            }
            
            if (_isoDrive != null)
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
            }

            MountBtn.IsEnabled = true;
            Log("全部卸载完成。");
        }
        catch (Exception ex)
        {
            Log($"卸载失败: {ex.Message}");
            MessageBox.Show($"卸载失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            UnmountBtn.IsEnabled = true;
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

            Log("✅ 所有更改已应用！请点击「卸载并提交」保存更改。");
            MessageBox.Show("所有更改已应用！\n\n请点击「图像」页面的「卸载并提交」按钮保存更改。", 
                "完成", MessageBoxButton.OK, MessageBoxImage.Information);
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
