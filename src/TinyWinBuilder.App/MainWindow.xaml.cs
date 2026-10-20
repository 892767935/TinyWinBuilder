using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using TinyWinBuilder.Core;

namespace TinyWinBuilder.App;

public partial class MainWindow : Window
{
    private readonly ImageManager _imageMgr = new();

    // 预装应用列表
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

    // 系统功能
    public List<FeatureItem> Features { get; set; } = new()
    {
        new() { Id = "Internet-Explorer-Optional-amd64", Name = "Internet Explorer 11" },
        new() { Id = "WindowsMediaPlayer", Name = "Windows Media Player" },
        new() { Id = "Windows-WordPad", Name = "写字板" },
        new() { Id = "Microsoft-Windows-StepsRecorder", Name = "步骤记录器" },
    };

    // 注册表 Tweaks
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
    }

    private void Nav_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (NavList.SelectedItem is not ListBoxItem item) return;
        var panels = new[] { ImagePanel, AppsPanel, FeaturesPanel, TweaksPanel, UnattendedPanel, ApplyPanel };
        foreach (var p in panels) p.Visibility = Visibility.Collapsed;

        var idx = NavList.SelectedIndex;
        if (idx >= 0 && idx < panels.Length)
            panels[idx].Visibility = Visibility.Visible;
    }

    private async void BrowseWim_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Filter = "WIM 镜像|*.wim|ISO 镜像|*.iso|所有文件|*.*" };
        if (dlg.ShowDialog() == true)
        {
            WimPathBox.Text = dlg.FileName;
            Log("正在读取镜像版本...");
            try
            {
                var indexes = await _imageMgr.GetImageIndexesAsync(dlg.FileName);
                ImageIndexCombo.ItemsSource = indexes;
                ImageIndexCombo.DisplayMemberPath = "Name";
                if (indexes.Count > 0) ImageIndexCombo.SelectedIndex = 0;
                Log($"找到 {indexes.Count} 个版本");
            }
            catch (Exception ex)
            {
                Log($"读取失败: {ex.Message}");
            }
        }
    }

    private async void Mount_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(WimPathBox.Text)) { Log("请先选择镜像文件"); return; }
        if (ImageIndexCombo.SelectedItem is not ImageInfo img) { Log("请选择镜像版本"); return; }

        var mountDir = Path.Combine(Path.GetTempPath(), "TinyWin_Mount");
        Log("正在挂载镜像...");
        try
        {
            await _imageMgr.MountAsync(WimPathBox.Text, img.Index, mountDir);
            Log("镜像挂载成功！现在可以离线修改。");
            MountBtn.IsEnabled = false;
            UnmountBtn.IsEnabled = true;
        }
        catch (Exception ex)
        {
            Log($"挂载失败: {ex.Message}");
        }
    }

    private async void Unmount_Click(object sender, RoutedEventArgs e)
    {
        Log("正在卸载镜像（提交更改）...");
        try
        {
            await _imageMgr.UnmountAsync(commit: true);
            Log("镜像卸载完成，更改已保存。");
            MountBtn.IsEnabled = true;
            UnmountBtn.IsEnabled = false;
        }
        catch (Exception ex)
        {
            Log($"卸载失败: {ex.Message}");
        }
    }

    private void PreviewUnattended_Click(object sender, RoutedEventArgs e)
    {
        var settings = new UnattendedSettings
        {
            Username = UaUsername.Text,
            Password = UaPassword.Password,
            AutoLoginCount = int.TryParse(UaAutoLogin.Text, out var n) ? n : 1
        };
        var xml = UnattendedGenerator.Generate(settings);
        var win = new Window
        {
            Title = "autounattend.xml 预览",
            Width = 700, Height = 500,
            Content = new TextBox
            {
                Text = xml, IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            }
        };
        win.ShowDialog();
    }

    private void SaveUnattended_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SaveFileDialog { Filter = "XML 文件|*.xml", FileName = "autounattend.xml" };
        if (dlg.ShowDialog() == true)
        {
            var settings = new UnattendedSettings
            {
                Username = UaUsername.Text,
                Password = UaPassword.Password,
                AutoLoginCount = int.TryParse(UaAutoLogin.Text, out var n) ? n : 1
            };
            var xml = UnattendedGenerator.Generate(settings);
            File.WriteAllText(dlg.FileName, xml);
            Log($"已保存: {dlg.FileName}");
        }
    }

    private async void Apply_Click(object sender, RoutedEventArgs e)
    {
        Log("=== 开始应用更改 ===");

        // 1. 移除应用
        Log("\n--- 移除预装应用 ---");
        foreach (var app in Apps.Where(a => a.IsChecked))
        {
            Log($"移除: {app.Name}...");
            // 在线环境用 PowerShell，离线环境用 DISM
            await RunPowerShell($"Get-AppxPackage -AllUsers | Where-Object {{ $_.Name -like \"*{app.Id}*\" }} | Remove-AppxPackage -AllUsers -ErrorAction SilentlyContinue");
            Log("  完成");
        }

        // 2. 禁用功能
        Log("\n--- 禁用系统功能 ---");
        foreach (var feat in Features.Where(f => f.IsChecked))
        {
            Log($"禁用: {feat.Name}...");
            await RunPowerShell($"dism /online /disable-feature /featurename:{feat.Id} /norestart /quiet");
            Log("  完成");
        }

        Log("\n=== 完成！建议重启电脑 ===");
    }

    private async Task RunPowerShell(string command, int timeout = 60)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -Command \"{command}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true
            };
            using var p = Process.Start(psi)!;
            p.WaitForExit(timeout * 1000);
        }
        catch { }
    }

    private void Log(string msg)
    {
        LogBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}\n");
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
