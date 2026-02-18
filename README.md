# TaskTango - Terminal-Inspired Notepad with Task Management

A modern, cross-platform notepad application that combines the simplicity of Windows Notepad with powerful task management capabilities, featuring a terminal-inspired Tango Dark theme and vim-inspired editing features.

## Features

### Core Functionality

- **Split-panel interface** - Notes on the left, tasks on the right
- **Multiple notepad pages** - Switch between different note sets
- **Real-time task management** - Add, edit, delete, and toggle tasks
- **Search and filtering** - Find tasks quickly with live search
- **Auto-save** - Never lose your work with automatic saving
- **Export to .txt** - Export notes and tasks to text files

### Advanced Features

- **Vim-inspired modal editing** - Normal/Insert/Command modes for power users
- **Tango Dark theme** - Terminal-inspired color scheme for comfortable viewing
- **Cross-platform** - Runs on Windows, macOS, and Linux
- **Standalone executable** - No installation required
- **JSON file storage** - Human-readable data format

### Keyboard Shortcuts

- **Ctrl+S** - Save current notepad
- **Ctrl+N** - Add new notepad page
- **Escape** - Exit insert mode (vim mode)
- **:w** - Save (vim command mode)
- **:q** - Quit (vim command mode)
- **:wq** - Save and quit (vim command mode)

## Installation

### Download Pre-built Binary

1. Go to the [Releases](https://github.com/yourusername/TaskTango/releases) page
2. Download the latest release for your platform
3. Extract the archive
4. Run `TaskTango.exe` (Windows) or `TaskTango` (macOS/Linux)

### Build from Source

```bash
# Clone the repository
git clone https://github.com/yourusername/TaskTango.git
cd TaskTango

# Build the application
dotnet build -c Release

# Run the application
dotnet run
```

### Create Self-contained Executable

```bash
# Windows
dotnet publish -c Release -r win-x64 --self-contained

# macOS
dotnet publish -c Release -r osx-x64 --self-contained

# Linux
dotnet publish -c Release -r linux-x64 --self-contained
```

## Development

### Prerequisites

- .NET 9.0 SDK
- Avalonia UI framework

### Project Structure

```txt
TaskTango/
├── Models/                 # Data models
│   ├── TaskItem.cs
│   ├── NotepadData.cs
│   └── AppSettings.cs
├── ViewModels/             # MVVM view models
│   └── MainWindowViewModel.cs
├── Views/                  # UI views
│   └── MainWindow.axaml
├── Services/               # Business logic
│   ├── FileStorageService.cs
│   └── VimModeService.cs
├── Converters/             # Value converters
│   └── BoolToStrikethroughConverter.cs
└── App.axaml               # Application resources and styles
```

### Running Tests

```bash
dotnet test
```

## Usage

### Basic Usage

1. **Create notes** - Type in the notes section on the left
2. **Add tasks** - Enter task text and click "Add" or press Enter
3. **Manage tasks** - Check off completed tasks, edit inline, or delete
4. **Search tasks** - Use the search box to filter tasks
5. **Switch pages** - Click the tabs to switch between notepad pages

### Vim Mode (Optional)

Enable vim-inspired editing for power users:

- Press `i` to enter insert mode
- Press `Escape` to return to normal mode
- Use `hjkl` for navigation
- Use `:` for command mode

### Data Storage

- **Location**: `%APPDATA%/TaskTango/settings.json` (Windows)
- **Format**: JSON (human-readable)
- **Backup**: Simply copy the settings file to backup your data

## Customization

### Theme

The application uses the Tango Dark color scheme. Colors can be modified in `App.axaml`:

```xml
<Color x:Key="TangoDarkBg">#2E3436</Color>
<Color x:Key="TangoDarkFg">#EEEEEC</Color>
<!-- Add more colors as needed -->
```

### Vim Commands

Add custom vim commands in `VimModeService.cs`:

```csharp
case "yourcommand":
    // Handle custom command
    break;
```

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature to TaskTango'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Development Guidelines

- Follow C# coding conventions
- Add comments for complex logic
- Test new features thoroughly
- Update documentation as needed

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- [Avalonia UI](https://avaloniaui.net/) - Cross-platform UI framework
- [CommunityToolkit.Mvvm](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/) - MVVM framework
- [Tango Desktop Project](https://tango.freedesktop.org/) - Color scheme inspiration

## Support

- **Issues**: [GitHub Issues](https://github.com/yourusername/TaskTango/issues)
- **Discussions**: [GitHub Discussions](https://github.com/yourusername/TaskTango/discussions)
- **Email**: your.email@example.com

## Changelog

### v1.0.0 (2025-02-18)

- Initial release of TaskTango
- Basic notepad and task management
- Tango Dark theme
- Vim-inspired editing
- Cross-platform support

---

Made with love for developers who love terminal aesthetics
