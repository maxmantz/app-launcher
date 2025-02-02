using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AppLauncher.Models;

namespace AppLauncher.Services
{
    /// <summary>
    /// Service responsible for saving and loading launch profiles to/from disk
    /// </summary>
    public class ProfileService
    {
        private readonly string _profilesPath;

        /// <summary>
        /// Initializes the service and ensures the application data directory exists
        /// </summary>
        public ProfileService()
        {
            // Create the application data directory in the user's AppData folder
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AppLauncher"
            );
            Directory.CreateDirectory(appDataPath);
            _profilesPath = Path.Combine(appDataPath, "profiles.json");
        }

        /// <summary>
        /// Saves the list of profiles to a JSON file
        /// </summary>
        /// <param name="profiles">The profiles to save</param>
        public async Task SaveProfilesAsync(IEnumerable<LaunchProfile> profiles)
        {
            // Create an anonymous type to control JSON serialization
            var profilesList = profiles.Select(p => new
            {
                p.Name,
                Processes = p.Processes.Select(proc => new
                {
                    proc.Name,
                    proc.ExecutablePath
                }).ToList()
            }).ToList();

            // Serialize with indentation for readability
            var json = JsonSerializer.Serialize(profilesList, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            await File.WriteAllTextAsync(_profilesPath, json);
        }

        /// <summary>
        /// Loads profiles from the JSON file
        /// </summary>
        /// <returns>The list of loaded profiles, or an empty list if the file doesn't exist</returns>
        public async Task<List<LaunchProfile>> LoadProfilesAsync()
        {
            if (!File.Exists(_profilesPath))
            {
                return new List<LaunchProfile>();
            }

            var json = await File.ReadAllTextAsync(_profilesPath);
            var profiles = JsonSerializer.Deserialize<List<LaunchProfile>>(json) ?? new List<LaunchProfile>();

            // Convert the loaded profiles to use ObservableCollection
            foreach (var profile in profiles)
            {
                var processes = profile.Processes.ToList();
                profile.Processes = new ObservableCollection<ProcessInfo>(processes);
            }

            return profiles;
        }
    }
}