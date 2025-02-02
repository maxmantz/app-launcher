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
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly ProfileService _profileService;
        private LaunchProfile? _selectedProfile;
        private ProcessInfo? _selectedProcess;

        /// <summary>
        /// Observable collection of all launch profiles
        /// </summary>
        public ObservableCollection<LaunchProfile> Profiles { get; } = new();

        /// <summary>
        /// Currently selected launch profile
        /// </summary>
        public LaunchProfile? SelectedProfile
        {
            get => _selectedProfile;
            set
            {
                // Unsubscribe from previous profile's collection changes
                if (_selectedProfile != null)
                {
                    _selectedProfile.Processes.CollectionChanged -= ProcessesCollectionChanged;
                }
                _selectedProfile = value;
                // Subscribe to new profile's collection changes
                if (_selectedProfile != null)
                {
                    _selectedProfile.Processes.CollectionChanged += ProcessesCollectionChanged;
                }
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsProfileSelected));
                OnPropertyChanged(nameof(LaunchButtonText));
            }
        }

        /// <summary>
        /// Currently selected process in the profile
        /// </summary>
        public ProcessInfo? SelectedProcess
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

        // Commands for UI interactions
        public ICommand AddProfileCommand { get; }
        public ICommand DeleteProfileCommand { get; }
        public ICommand AddProcessCommand { get; }
        public ICommand DeleteProcessCommand { get; }
        public ICommand LaunchProfileCommand { get; }
        public ICommand BrowseExecutableCommand { get; }

        public MainViewModel()
        {
            _profileService = new ProfileService();

            // Initialize commands
            AddProfileCommand = new RelayCommand(AddProfile);
            DeleteProfileCommand = new RelayCommand(DeleteProfile, () => IsProfileSelected);
            AddProcessCommand = new RelayCommand(AddProcess, () => IsProfileSelected);
            DeleteProcessCommand = new RelayCommand(DeleteProcess, () => IsProcessSelected);
            LaunchProfileCommand = new RelayCommand(ToggleProfileExecution, () => IsProfileSelected);
            BrowseExecutableCommand = new RelayCommand(BrowseExecutable, () => IsProcessSelected);

            LoadProfiles();
        }

        /// <summary>
        /// Toggles the execution state of the selected profile
        /// </summary>
        private void ToggleProfileExecution()
        {
            if (SelectedProfile == null) return;

            if (SelectedProfile.IsRunning)
            {
                StopProfile(SelectedProfile);
            }
            else
            {
                LaunchProfile();
            }
        }

        /// <summary>
        /// Stops all processes in the specified profile
        /// </summary>
        private void StopProfile(LaunchProfile profile)
        {
            foreach (var process in profile.Processes)
            {
                try
                {
                    if (process.RunningProcess != null && !process.RunningProcess.HasExited)
                    {
                        process.RunningProcess.Kill(true);
                        process.RunningProcess.Dispose();
                        process.RunningProcess = null;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to stop process {process.Name}: {ex.Message}",
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
                profile.Processes.CollectionChanged += ProcessesCollectionChanged;
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
            var profile = new LaunchProfile { Name = "New Profile" };
            profile.Processes.CollectionChanged += ProcessesCollectionChanged;
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
            SelectedProfile.Processes.CollectionChanged -= ProcessesCollectionChanged;
            Profiles.Remove(SelectedProfile);
            SaveProfiles();
        }

        /// <summary>
        /// Adds a new process to the selected profile
        /// </summary>
        private void AddProcess()
        {
            if (SelectedProfile == null) return;
            var process = new ProcessInfo { Name = "New Process" };
            SelectedProfile.Processes.Add(process);
            SelectedProcess = process;
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
            SelectedProfile.Processes.Remove(SelectedProcess);
        }

        /// <summary>
        /// Launches all processes in the selected profile
        /// </summary>
        private void LaunchProfile()
        {
            if (SelectedProfile == null) return;

            bool anyProcessStarted = false;
            foreach (var process in SelectedProfile.Processes)
            {
                if (string.IsNullOrEmpty(process.ExecutablePath)) continue;
                try
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = process.ExecutablePath,
                        UseShellExecute = true
                    };

                    process.RunningProcess = Process.Start(startInfo);
                    if (process.RunningProcess != null)
                    {
                        anyProcessStarted = true;
                        process.RunningProcess.EnableRaisingEvents = true;
                        // Handle process exit
                        process.RunningProcess.Exited += (s, e) =>
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                process.RunningProcess?.Dispose();
                                process.RunningProcess = null;
                                CheckIfProfileStillRunning(SelectedProfile);
                            });
                        };
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to start process {process.Name}: {ex.Message}",
                                  "Error",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Warning);
                }
            }

            if (anyProcessStarted)
            {
                SelectedProfile.IsRunning = true;
                OnPropertyChanged(nameof(LaunchButtonText));
            }
        }

        /// <summary>
        /// Checks if any processes in the profile are still running
        /// </summary>
        private void CheckIfProfileStillRunning(LaunchProfile profile)
        {
            bool isStillRunning = profile.Processes.Any(p => p.RunningProcess != null && !p.RunningProcess.HasExited);
            if (!isStillRunning)
            {
                profile.IsRunning = false;
                OnPropertyChanged(nameof(LaunchButtonText));
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
                SelectedProcess.ExecutablePath = dialog.FileName;
                var fileName = Path.GetFileNameWithoutExtension(dialog.FileName);
                if (string.IsNullOrEmpty(SelectedProcess.Name) || SelectedProcess.Name == "New Process")
                {
                    SelectedProcess.Name = fileName;
                }
                SaveProfiles();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
}