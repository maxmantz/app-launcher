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

            // Set shutdown mode to close app when main window closes
            this.ShutdownMode = ShutdownMode.OnMainWindowClose;

#if DEBUG
            // Create a console window for debug output only in Debug mode
            // AllocConsole();  // Comment out or remove this line to disable the console window
            Console.WriteLine("Debug console initialized.");
#endif
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

            // Show the main window
            MainWindow = new MainWindow();
            MainWindow.Show();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in OnStartup: {ex}");
            MessageBox.Show($"Startup error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Ensure all child processes are stopped before exiting
        foreach (var window in Windows.OfType<MainWindow>())
        {
            var viewModel = window.DataContext as ViewModels.MainViewModel;
            viewModel?.StopAllProfiles();
        }

#if DEBUG
        // Free the debug console if it was allocated
        FreeConsole();
#endif

        base.OnExit(e);
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

    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern bool FreeConsole();
}

