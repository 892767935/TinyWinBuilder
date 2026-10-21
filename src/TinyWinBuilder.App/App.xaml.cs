using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace TinyWinBuilder.App;

public partial class App : Application
{
    private static readonly string LogPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "error.log");

    public App()
    {
        this.DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
    }

    private void LogError(string source, Exception ex)
    {
        try
        {
            var msg = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {source}\n{ex}\n\n";
            File.AppendAllText(LogPath, msg);
        }
        catch { }
    }

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LogError("UI 异常", e.Exception);
        MessageBox.Show($"UI 异常: {e.Exception.Message}\n\n{e.Exception.StackTrace}\n\n错误已记录到: {LogPath}", 
            "TinyWin Builder 错误", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            LogError("系统异常", ex);
            MessageBox.Show($"系统异常: {ex.Message}\n\n{ex.StackTrace}\n\n错误已记录到: {LogPath}", 
                "TinyWin Builder 错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
