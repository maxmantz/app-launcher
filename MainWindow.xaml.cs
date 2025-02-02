using System;
using System.Windows;

namespace AppLauncher;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        try
        {
            Console.WriteLine("Initializing MainWindow...");
            InitializeComponent();
            Console.WriteLine("MainWindow initialized successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing MainWindow: {ex}");
            MessageBox.Show($"Error initializing window: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}