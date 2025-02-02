using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace AppLauncher.Models
{
    /// <summary>
    /// Represents a launch profile that contains a collection of processes to be launched together
    /// </summary>
    public class LaunchProfile : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private ObservableCollection<ProcessInfo> _processes = new();
        private bool _isRunning;

        /// <summary>
        /// The name of the launch profile
        /// </summary>
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Collection of processes included in this profile
        /// </summary>
        public ObservableCollection<ProcessInfo> Processes
        {
            get => _processes;
            set
            {
                _processes = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Indicates whether the profile is currently running
        /// Not serialized to JSON as it's a runtime state
        /// </summary>
        [JsonIgnore]
        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                _isRunning = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Represents a process that can be launched as part of a profile
    /// </summary>
    public class ProcessInfo : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private string _executablePath = string.Empty;
        [JsonIgnore]
        private System.Diagnostics.Process? _runningProcess;

        /// <summary>
        /// The display name of the process
        /// </summary>
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// The full path to the executable file
        /// </summary>
        public string ExecutablePath
        {
            get => _executablePath;
            set
            {
                _executablePath = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Reference to the running process instance
        /// Not serialized to JSON as it's a runtime state
        /// </summary>
        [JsonIgnore]
        public System.Diagnostics.Process? RunningProcess
        {
            get => _runningProcess;
            set
            {
                _runningProcess = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}