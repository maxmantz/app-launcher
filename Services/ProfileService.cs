using System.IO;
using System.Text.Json;
using AppLauncher.Models;

namespace AppLauncher.Services
{
    /// <summary>
    /// Service responsible for saving and loading launch profiles to/from disk
    /// </summary>
    public class ProfileService
    {
        private readonly string _profilesPath;
        private Task? _lastSaveTask;
        private readonly object _saveLock = new object();

        /// <summary>
        /// Initializes the service and ensures the application data directory exists
        /// </summary>
        public ProfileService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appDataPath, "AppLauncher");
            _profilesPath = Path.Combine(appFolder, "profiles.json");

            // Create directory if it doesn't exist
            Directory.CreateDirectory(appFolder);
        }

        /// <summary>
        /// Saves the list of profiles to a JSON file
        /// </summary>
        /// <param name="profiles">The profiles to save</param>
        public async Task SaveProfilesAsync(IEnumerable<Profile> profiles)
        {
            lock (_saveLock)
            {
                // If there's a save in progress, wait for it to complete
                _lastSaveTask?.Wait();

                // Start new save operation
                _lastSaveTask = SaveProfilesInternalAsync(profiles);
            }

            await _lastSaveTask;
        }

        private async Task SaveProfilesInternalAsync(IEnumerable<Profile> profiles)
        {
            var json = JsonSerializer.Serialize(profiles, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            // Use FileShare.None to ensure exclusive access
            using var fileStream = new FileStream(_profilesPath, FileMode.Create, FileAccess.Write, FileShare.None);
            using var writer = new StreamWriter(fileStream);
            await writer.WriteAsync(json);
        }

        /// <summary>
        /// Loads profiles from the JSON file
        /// </summary>
        /// <returns>The list of loaded profiles, or an empty list if the file doesn't exist</returns>
        public async Task<List<Profile>> LoadProfilesAsync()
        {
            if (!File.Exists(_profilesPath))
            {
                return new List<Profile>();
            }

            try
            {
                var json = await File.ReadAllTextAsync(_profilesPath);
                var profiles = JsonSerializer.Deserialize<List<Profile>>(json);
                return profiles ?? new List<Profile>();
            }
            catch
            {
                return new List<Profile>();
            }
        }
    }
}