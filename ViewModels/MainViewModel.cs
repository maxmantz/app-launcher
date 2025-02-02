using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using AppLauncher.Models;
using AppLauncher.Services;
using Microsoft.Win32;

namespace AppLauncher.ViewModels
{
    /// <summary>
    /// Main view model that handles the application's logic and state
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        private readonly ProfileService _profileService;
        private Profile? _selectedProfile;
        private Models.Application? _selectedProcess;
        private bool _isLaunching;

        /// <summary>
        /// Observable collection of all launch profiles
        /// </summary>
        public ObservableCollection<Profile> Profiles { get; } = new();

        /// <summary>
        /// Currently selected launch profile
        /// </summary>
        public Profile? SelectedProfile
        {
            get => _selectedProfile;
            set
            {
                if (_selectedProfile != null)
                {
                    _selectedProfile.Applications.CollectionChanged -= ProcessesCollectionChanged;
                }
                _selectedProfile = value;
                if (_selectedProfile != null)
                {
                    _selectedProfile.Applications.CollectionChanged += ProcessesCollectionChanged;
                }
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsProfileSelected));
                OnPropertyChanged(nameof(LaunchButtonText));
            }
        }

        /// <summary>
        /// Currently selected process in the profile
        /// </summary>
        public Models.Application? SelectedProcess
        {
            get => _selectedProcess;
            set
            {
                _selectedProcess = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsProcessSelected));
            }
        }

        /// <summary>
        /// Indicates if a profile is currently selected
        /// </summary>
        public bool IsProfileSelected => SelectedProfile != null;

        /// <summary>
        /// Indicates if a process is currently selected
        /// </summary>
        public bool IsProcessSelected => SelectedProcess != null;

        /// <summary>
        /// Text for the launch button that changes based on profile state
        /// </summary>
        public string LaunchButtonText => SelectedProfile?.IsRunning == true ? "Stop Profile" : "Launch Profile";

        public bool IsLaunching
        {
            get => _isLaunching;
            set
            {
                _isLaunching = value;
                OnPropertyChanged();
            }
        }

        // Commands for UI interactions
        public ICommand AddProfileCommand { get; }
        public ICommand DeleteProfileCommand { get; }
        public ICommand AddProcessCommand { get; }
        public ICommand DeleteProcessCommand { get; }
        public RelayCommand LaunchProfileCommand { get; }
        public ICommand BrowseExecutableCommand { get; }

        public MainViewModel()
        {
            _profileService = new ProfileService();

            // Initialize commands
            AddProfileCommand = new RelayCommand(AddProfile);
            DeleteProfileCommand = new RelayCommand(DeleteProfile, () => IsProfileSelected);
            AddProcessCommand = new RelayCommand(AddProcess, () => IsProfileSelected);
            DeleteProcessCommand = new RelayCommand(DeleteProcess, () => IsProcessSelected);
            LaunchProfileCommand = new RelayCommand(LaunchProfile, () => IsProfileSelected);
            BrowseExecutableCommand = new RelayCommand(BrowseExecutable, () => IsProcessSelected);

            LoadProfiles();
        }

        /// <summary>
        /// Stops all processes in the specified profile
        /// </summary>
        private void StopProfile(Profile profile)
        {
            foreach (var app in profile.Applications)
            {
                try
                {
                    // First try to stop the process if we have a direct reference
                    if (app.RunningProcess != null && !app.RunningProcess.HasExited)
                    {
                        app.RunningProcess.Kill(true);
                        app.RunningProcess.Dispose();
                        app.RunningProcess = null;
                        continue;
                    }

                    // If we don't have a direct reference or it's already exited,
                    // search for processes by executable name
                    if (!string.IsNullOrEmpty(app.Path))
                    {
                        var processName = Path.GetFileNameWithoutExtension(app.Path);
                        var processes = Process.GetProcessesByName(processName);

                        foreach (var process in processes)
                        {
                            try
                            {
                                if (!process.HasExited)
                                {
                                    process.Kill(true);
                                }
                            }
                            finally
                            {
                                process.Dispose();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to stop process {app.Name}: {ex.Message}",
                                  "Error",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Warning);
                }
            }
            profile.IsRunning = false;
            OnPropertyChanged(nameof(LaunchButtonText));
        }

        /// <summary>
        /// Handles changes to the processes collection and saves changes
        /// </summary>
        private void ProcessesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            SaveProfiles();
        }

        /// <summary>
        /// Loads profiles from disk
        /// </summary>
        private async void LoadProfiles()
        {
            var profiles = await _profileService.LoadProfilesAsync();
            Profiles.Clear();
            foreach (var profile in profiles)
            {
                profile.Applications.CollectionChanged += ProcessesCollectionChanged;
                Profiles.Add(profile);
            }
        }

        /// <summary>
        /// Saves profiles to disk
        /// </summary>
        private async void SaveProfiles()
        {
            await _profileService.SaveProfilesAsync(Profiles.ToList());
        }

        /// <summary>
        /// Adds a new profile
        /// </summary>
        private void AddProfile()
        {
            var profile = new Profile { Name = "New Profile" };
            profile.Applications.CollectionChanged += ProcessesCollectionChanged;
            Profiles.Add(profile);
            SelectedProfile = profile;
            SaveProfiles();
        }

        /// <summary>
        /// Deletes the selected profile
        /// </summary>
        private void DeleteProfile()
        {
            if (SelectedProfile == null) return;
            if (SelectedProfile.IsRunning)
            {
                StopProfile(SelectedProfile);
            }
            SelectedProfile.Applications.CollectionChanged -= ProcessesCollectionChanged;
            Profiles.Remove(SelectedProfile);
            SaveProfiles();
        }

        /// <summary>
        /// Adds a new process to the selected profile
        /// </summary>
        private void AddProcess()
        {
            if (SelectedProfile == null) return;
            var app = new Models.Application { Name = "New Process" };
            SelectedProfile.Applications.Add(app);
            SelectedProcess = app;
        }

        /// <summary>
        /// Deletes the selected process
        /// </summary>
        private void DeleteProcess()
        {
            if (SelectedProfile == null || SelectedProcess == null) return;
            if (SelectedProcess.RunningProcess != null && !SelectedProcess.RunningProcess.HasExited)
            {
                try
                {
                    SelectedProcess.RunningProcess.Kill(true);
                    SelectedProcess.RunningProcess.Dispose();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to stop process {SelectedProcess.Name}: {ex.Message}",
                                  "Error",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Warning);
                }
            }
            SelectedProfile.Applications.Remove(SelectedProcess);
        }

        /// <summary>
        /// Checks if a process is running
        /// </summary>
        /// <param name="processName"></param>
        /// <returns></returns>
        private bool IsProcessRunning(string processName)
        {
            Process runningProcess = Process.GetProcessesByName(processName).FirstOrDefault();

            return runningProcess != null && !runningProcess.HasExited;
        }

        /// <summary>
        /// Launches a profile
        /// </summary>
        /// <param name="profile"></param>
        /// <returns></returns>
        public async void LaunchProfile()
        {
            if (SelectedProfile == null) return;
            IsLaunching = true;

            try
            {
                if (SelectedProfile.IsRunning)
                {
                    StopProfile(SelectedProfile);
                    return;
                }

                var launchTasks = SelectedProfile.Applications
                    .Where(app => !string.IsNullOrEmpty(app.Path) &&
                                !IsProcessRunning(Path.GetFileNameWithoutExtension(app.Path)))
                    .Select(async app =>
                    {
                        try
                        {
                            var startInfo = new ProcessStartInfo
                            {
                                FileName = app.Path,
                                Arguments = app.Arguments,
                                UseShellExecute = false
                            };

                            await Task.Run(() =>
                            {
                                var runningProcess = Process.Start(startInfo);
                                app.RunningProcess = runningProcess;
                            });
                        }
                        catch (Exception ex)
                        {
                            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                MessageBox.Show($"Failed to start process {app.Name}: {ex.Message}",
                                              "Error",
                                              MessageBoxButton.OK,
                                              MessageBoxImage.Warning);
                            });
                        }
                    });

                await Task.WhenAll(launchTasks);
                SelectedProfile.IsRunning = true;
                OnPropertyChanged(nameof(LaunchButtonText));
            }
            finally
            {
                IsLaunching = false;
            }
        }

        /// <summary>
        /// Opens a file dialog to browse for an executable
        /// </summary>
        private void BrowseExecutable()
        {
            if (SelectedProcess == null) return;
            var dialog = new OpenFileDialog
            {
                Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*",
                Title = "Select Executable"
            };

            if (dialog.ShowDialog() == true)
            {
                SelectedProcess.Path = dialog.FileName;
                var fileName = Path.GetFileNameWithoutExtension(dialog.FileName);
                if (string.IsNullOrEmpty(SelectedProcess.Name) || SelectedProcess.Name == "New Process")
                {
                    SelectedProcess.Name = fileName;
                }
                SaveProfiles();
            }
        }

        public void StopAllProfiles()
        {
            foreach (var profile in Profiles.Where(p => p.IsRunning))
            {
                StopProfile(profile);
            }
        }
    }

    /// <summary>
    /// Basic implementation of ICommand for handling UI commands
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object? parameter) => _execute();
    }

    public class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class AsyncRelayCommand : ICommand
    {
        private readonly Func<Task> _execute;
        private readonly Func<bool>? _canExecute;
        private bool _isExecuting;

        public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter)
        {
            return !_isExecuting && (_canExecute?.Invoke() ?? true);
        }

        public async void Execute(object? parameter)
        {
            if (_isExecuting) return;

            try
            {
                _isExecuting = true;
                CommandManager.InvalidateRequerySuggested();
                await _execute();
            }
            finally
            {
                _isExecuting = false;
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }
}