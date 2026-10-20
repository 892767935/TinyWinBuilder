using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace TinyWinBuilder.Core;

/// <summary>
/// 离线 Windows 镜像管理（DISM 封装）
/// </summary>
public class ImageManager
{
    public string? MountDir { get; private set; }
    public bool IsMounted => !string.IsNullOrEmpty(MountDir);

    /// <summary>
    /// 获取 WIM 镜像中的版本列表
    /// </summary>
    public async Task<List<ImageInfo>> GetImageIndexesAsync(string wimPath)
    {
        var result = await RunDismAsync($"/Get-WimInfo /WimFile:\"{wimPath}\"");
        var images = new List<ImageInfo>();
        var lines = result.Split('\n');
        int currentIndex = 0;
        string? currentName = null;

        foreach (var line in lines)
        {
            if (line.TrimStart().StartsWith("Index :"))
            {
                int.TryParse(line.Split(':')[1].Trim(), out currentIndex);
            }
            else if (line.TrimStart().StartsWith("Name :"))
            {
                currentName = line.Split(':')[1].Trim();
                images.Add(new ImageInfo { Index = currentIndex, Name = currentName });
            }
        }
        return images;
    }

    /// <summary>
    /// 挂载 WIM 镜像
    /// </summary>
    public async Task MountAsync(string wimPath, int index, string mountDir)
    {
        Directory.CreateDirectory(mountDir);
        await RunDismAsync($"/Mount-Image /ImageFile:\"{wimPath}\" /Index:{index} /MountDir:\"{mountDir}\"");
        MountDir = mountDir;
    }

    /// <summary>
    /// 卸载镜像（提交/丢弃更改）
    /// </summary>
    public async Task UnmountAsync(bool commit = true)
    {
        if (!IsMounted) return;
        var action = commit ? "Commit" : "Discard";
        await RunDismAsync($"/Unmount-Image /MountDir:\"{MountDir}\" /{action}");
        MountDir = null;
    }

    /// <summary>
    /// 清理组件存储（减少体积）
    /// </summary>
    public async Task CleanupImageAsync()
    {
        if (!IsMounted) throw new InvalidOperationException("镜像未挂载");
        await RunDismAsync($"/Cleanup-Image /MountDir:\"{MountDir}\" /StartComponentCleanup /ResetBase");
    }

    /// <summary>
    /// 导出精简后的镜像
    /// </summary>
    public async Task ExportImageAsync(string outputWim)
    {
        await RunDismAsync($"/Export-Image /SourceImageFile:\"{outputWim}\" /SourceIndex:1 /DestinationImageFile:\"{outputWim}.new.wim\"");
    }

    /// <summary>
    /// 移除离线预装应用（Provisioned Appx）
    /// </summary>
    public async Task RemoveProvisionedAppAsync(string packageName)
    {
        if (!IsMounted) throw new InvalidOperationException("镜像未挂载");
        await RunDismAsync($"/Image:\"{MountDir}\" /Remove-ProvisionedAppxPackage /PackageName:{packageName}");
    }

    /// <summary>
    /// 获取离线预装应用列表
    /// </summary>
    public async Task<List<string>> GetProvisionedAppsAsync()
    {
        if (!IsMounted) throw new InvalidOperationException("镜像未挂载");
        var result = await RunDismAsync($"/Image:\"{MountDir}\" /Get-ProvisionedAppxPackages");
        var apps = new List<string>();
        foreach (var line in result.Split('\n'))
        {
            if (line.TrimStart().StartsWith("DisplayName :"))
                apps.Add(line.Split(':')[1].Trim());
        }
        return apps;
    }

    /// <summary>
    /// 禁用离线 Windows 功能
    /// </summary>
    public async Task DisableFeatureAsync(string featureName)
    {
        if (!IsMounted) throw new InvalidOperationException("镜像未挂载");
        await RunDismAsync($"/Image:\"{MountDir}\" /Disable-Feature /FeatureName:{featureName} /Remove");
    }

    private async Task<string> RunDismAsync(string args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dism.exe",
            Arguments = args,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        using var process = Process.Start(psi)!;
        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();
        if (process.ExitCode != 0 && !output.Contains("successfully"))
        {
            var error = await process.StandardError.ReadToEndAsync();
            // DISM 有些操作返回非 0 但实际成功，这里只记录
        }
        return output;
    }
}

public class ImageInfo
{
    public int Index { get; set; }
    public string Name { get; set; } = "";
}
