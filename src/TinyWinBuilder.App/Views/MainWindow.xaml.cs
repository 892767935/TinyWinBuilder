using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using TinyWinBuilder.Core.Imaging;
using TinyWinBuilder.Core.Unattended;

namespace TinyWinBuilder.App.Views;

public partial class MainWindow : Window
{
    private readonly ImageManager _imageMgr = new();

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
    }

    private void NavList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (NavList.SelectedItem is not ListBoxItem) return;
        
        // XAML 加载时控件可能还没初始化，先检查空值
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
        var dlg = new OpenFileDialog { Filter = "WIM 镜像|*.wim|ISO 镜像|*.iso|所有文件|*.*" };
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
            MessageBox.Show("请先选择 WIM 镜像文件！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            Log("正在挂载镜像...");
            await _imageMgr.MountAsync(WimPathBox.Text, 1, Path.Combine(Path.GetTempPath(), "TinyWinBuilder_Mount"));
            MountBtn.IsEnabled = false;
            UnmountBtn.IsEnabled = true;
            Log("镜像挂载成功！");
        }
        catch (Exception ex)
        {
            Log($"挂载失败: {ex.Message}");
            MessageBox.Show($"挂载失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void Unmount_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Log("正在卸载镜像并提交更改...");
            await _imageMgr.UnmountAsync(true);
            MountBtn.IsEnabled = true;
            UnmountBtn.IsEnabled = false;
            Log("镜像已卸载，更改已提交。");
        }
        catch (Exception ex)
        {
            Log($"卸载失败: {ex.Message}");
            MessageBox.Show($"卸载失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void PreviewUnattended_Click(object sender, RoutedEventArgs e)
    {
        Log("预览自动部署 XML...");
        MessageBox.Show("自动部署 XML 预览功能开发中...", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void SaveUnattended_Click(object sender, RoutedEventArgs e)
    {
        Log("保存 autounattend.xml...");
        MessageBox.Show("保存自动部署文件功能开发中...", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        Log("应用更改功能开发中...");
        MessageBox.Show("应用更改功能开发中...", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
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
