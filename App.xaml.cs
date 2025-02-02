using System;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace AppLauncher;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public App()
    {
        Console.WriteLine("Application starting...");
        try
        {
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            // Create a console window for debug output
            AllocConsole();
            Console.WriteLine("Debug console initialized.");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Startup error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        Console.WriteLine("OnStartup called...");
        try
        {
            base.OnStartup(e);
            Console.WriteLine("Base OnStartup completed.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in OnStartup: {ex}");
            MessageBox.Show($"Startup error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LogError("Dispatcher Unhandled Exception", e.Exception);
        MessageBox.Show($"An error occurred: {e.Exception.Message}\n\nStack Trace:\n{e.Exception.StackTrace}",
                      "Application Error",
                      MessageBoxButton.OK,
                      MessageBoxImage.Error);
        e.Handled = true;
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            LogError("AppDomain Unhandled Exception", ex);
            MessageBox.Show($"A fatal error occurred: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}",
                          "Fatal Error",
                          MessageBoxButton.OK,
                          MessageBoxImage.Error);
        }
    }

    private void LogError(string type, Exception ex)
    {
        try
        {
            var logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AppLauncher",
                "error.log"
            );

            Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
            var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {type}\n{ex}\n\n";
            File.AppendAllText(logPath, logMessage);
            Console.WriteLine($"Error logged: {type}\n{ex}");
        }
        catch (Exception logEx)
        {
            Console.WriteLine($"Failed to log error: {logEx}");
        }
    }

    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern bool AllocConsole();
}

