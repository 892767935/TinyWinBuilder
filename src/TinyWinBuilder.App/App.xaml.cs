using System;
using System.Windows;
using System.Windows.Threading;

namespace TinyWinBuilder.App;

public partial class App : Application
{
    public App()
    {
        // 全局异常处理
        this.DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
    }

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show($"UI 异常: {e.Exception.Message}\n\n{e.Exception.StackTrace}", 
            "TinyWin Builder 错误", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            MessageBox.Show($"系统异常: {ex.Message}\n\n{ex.StackTrace}", 
                "TinyWin Builder 错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
