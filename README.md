# AppLauncher

AppLauncher is a Windows desktop application that helps you manage and launch groups of applications together. It allows you to create launch profiles, each containing multiple applications that can be started or stopped with a single click.

## Features

- **Launch Profiles**: Create and manage groups of applications that need to be launched together
- **Easy Management**: Add, edit, or remove applications from profiles
- **Quick Launch**: Start all applications in a profile with one click
- **Process Control**: Stop all running applications in a profile when needed
- **Automatic Process Tracking**: Monitors the state of running applications
- **Persistent Storage**: Profiles are automatically saved and loaded between sessions
- **User-Friendly Interface**: Clean and intuitive WPF-based user interface

## Requirements

- Windows operating system
- .NET 8.0 Runtime (included in self-contained deployments)

## Installation

1. Download the latest release from the releases page
2. Extract the zip file to your desired location
3. Run `AppLauncher.exe`

## Usage

### Creating a Launch Profile

1. Click "Add Profile" to create a new profile
2. Enter a name for your profile
3. Click "Add Process" to add an application to the profile
4. Use the "Browse" button to select the executable file
5. Repeat for all applications you want in the profile

### Managing Profiles

- **Launch Profile**: Click the "Launch Profile" button to start all applications in the profile
- **Stop Profile**: When a profile is running, click "Stop Profile" to close all applications
- **Edit Profile**: Change the profile name or modify its applications at any time
- **Delete Profile**: Select a profile and click "Delete Profile" to remove it

### Managing Processes

- **Add Process**: Click "Add Process" to add a new application to the selected profile
- **Remove Process**: Select a process and click "Delete Process" to remove it from the profile
- **Browse**: Use the "Browse" button to select an executable file for each process

## Development

### Prerequisites

- .NET 8.0 SDK
- Visual Studio 2022 or later (recommended)

### Building from Source

1. Clone the repository:
   ```bash
   git clone [repository-url]
   ```

2. Open the solution in Visual Studio or build from command line:
   ```bash
   dotnet build
   ```

3. Run the application:
   ```bash
   dotnet run
   ```

### Publishing

To create a self-contained deployment:
```bash
dotnet publish -c Release -p:PublishProfile=WinX64
```

The published application will be available in `bin\Release\publish\win-x64\`.

## Technical Details

- Built with .NET 8.0 and WPF
- Uses MVVM architecture pattern
- Stores profiles in JSON format in the user's AppData folder
- Self-contained deployment option available

## Project Structure

- **Models**: Contains `LaunchProfile` and `ProcessInfo` classes
- **ViewModels**: Contains the main `MainViewModel` for UI logic
- **Services**: Contains `ProfileService` for data persistence
- **Views**: Contains the WPF user interface

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request. 