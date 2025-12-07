# EasyNSIS

A modern GUI application for creating Windows installers using NSIS (Nullsoft Scriptable Install System).

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)
![Platform](https://img.shields.io/badge/Platform-Windows%20x64-0078D4)
![License](https://img.shields.io/badge/License-MIT-green)

[日本語](README.md)

## Features

- **Visual Installer Configuration** - Configure all installer settings through an intuitive modern UI
- **Windows Theme Support** - Automatically adapts to Light/Dark theme
- **Card-based Layout** - Clean section separation with header backgrounds
- **NSIS Script Generation** - Automatically generates NSIS scripts with Unicode support
- **Multiple Install Locations** - Support for AppData, Program Files, or custom paths
- **Shortcut Management** - Create desktop and start menu shortcuts (with automatic icon assignment)
- **License Configuration** - Include default, custom file, or inline license text
- **Multi-language Support** - UI available in English and Japanese
- **Configuration Save/Load** - Save and restore your installer configurations as XML files

## Screenshots

![image-20251207230834133](./Assets/image-20251207230834133.png)

## Requirements

- Windows 10/11 (x64)
- .NET 10.0 Runtime (included in self-contained build)
- NSIS 3.11 (included in distribution)

## Installation

1. Download the latest release from [Releases](../../releases)
2. Extract to your preferred location
3. Run `EasyNSIS.exe`

## Building from Source

### Prerequisites

- .NET 10.0 SDK
- Visual Studio 2022 or VS Code with C# extension
- NSIS 3.11

### NSIS Setup

To run EasyNSIS, you need to set up NSIS 3.11 as follows:

1. Download NSIS:
   ```
   https://downloads.sourceforge.net/project/nsis/NSIS%203/3.11/nsis-3.11.zip
   ```

2. Extract the downloaded zip file

3. Place the extracted folder in the following directory structure:
   ```
   EasyNSIS/
   └── Tools/
       └── nsis-3.11/
           ├── makensis.exe    ← This file is required
           ├── Contrib/
           ├── Include/
           ├── Plugins/
           └── ...
   ```

**Important**: If `Tools/nsis-3.11/makensis.exe` does not exist, the application will display an error at startup.

### Build Steps

```bash
# Clone the repository
git clone https://github.com/yourusername/EasyNSIS.git
cd EasyNSIS

# Restore dependencies
dotnet restore

# Build
dotnet build

# Run
dotnet run
```

### Publish

```bash
# Create self-contained executable
dotnet publish -c Release
```

## Usage

### Basic Workflow

1. **Basic Info** - Enter company name, application name, and version
2. **Source Folder** - Select the folder containing files to install
3. **Install Destination** - Choose where the application will be installed
4. **Shortcuts** - Configure desktop and start menu shortcuts
5. **License** - Select license type and content
6. **Output** - Choose output folder for the generated installer
7. **Post-Install** - Configure actions after installation (optional)
8. **Build** - Click the Build button to generate the installer

---

## Tab Details

### 1. Basic Info

Configure the basic information for your installer.

| Field | Description | Required |
|-------|-------------|:--------:|
| **Company Name** | Company name displayed in the installer and "Apps & Features". Max 64 characters. | Yes |
| **Application Name** | Name of the application to install. Also used as the installation folder name. Max 64 characters. | Yes |
| **Version** | Application version in Major.Minor.Build.Revision format (e.g., 1.0.0.0). Each segment: 0-65535. | Yes |

#### Language Settings

| Option | Description |
|--------|-------------|
| **Use System Locale** | Match installer language to user's system settings (recommended) |
| **Fixed Language** | Lock installer language to Japanese (ja-JP) or English (en-US) |

#### Registration Mode

| Option | Description |
|--------|-------------|
| **Register to Apps & Features** | Register in Windows "Apps & Features" (Add/Remove Programs). Allows standard uninstallation. Creates uninstaller and registry entries for proper Windows integration. |
| **Do not use Registry** | Disable registry writes and "Apps & Features" registration. For portable applications. Uninstaller is placed in the installation folder for manual execution. |

#### Installer Icon

Set the icon for the installer executable (.exe). Specify an .ico file. If not set, NSIS default icon is used.

---

### 2. Source Folder

Specify the folder containing files to be installed.

| Field | Description |
|-------|-------------|
| **Source Folder Path** | Directory path containing files and folders to install |

**Notes:**
- All files and subfolders within the specified folder will be included
- Folder structure is preserved
- Empty folders are not included
- Hidden and system files are included

**Reserved File Names:**
The following file names are used internally by EasyNSIS and cannot be included in the source folder:
- `installed_files.dat` - Installed files list
- `installed_version.dat` - Installed version information
- `Uninstall.exe` - Uninstaller executable

---

### 3. Install Destination

Configure where the application will be installed.

#### Default Install Location

| Option | Example Path | Admin Required |
|--------|--------------|:--------------:|
| **AppData (Roaming)** | `C:\Users\<user>\AppData\Roaming\<Company>\<App>` | No |
| **AppData (Local)** | `C:\Users\<user>\AppData\Local\<Company>\<App>` | No |
| **Program Files** | `C:\Program Files\<Company>\<App>` | **Yes** |
| **Custom** | Any path | Depends |

**Recommendations:**
- Per-user applications → **AppData (Roaming)** or **AppData (Local)**
- System-wide installation → **Program Files**
- Portable applications → **Custom**

#### Allow User to Change Install Path

When checked, users can modify the installation path during installation.

#### Uninstall Cleanup

| Option | Description |
|--------|-------------|
| **Delete All** | Delete all files and folders in the installation directory |
| **Preserve User Data** | Only delete files placed during installation, keep user-created files (configs, logs, etc.) |

#### File Management Mechanism

In "Preserve User Data" mode, EasyNSIS tracks files placed during installation and manages them precisely during uninstallation and upgrades.

**Files Generated During Installation:**

| File | Description |
|------|-------------|
| `installed_files.dat` | List of all installed files and directories. Recorded in `FILE:relative_path` or `DIR:relative_path` format. |
| `installed_version.dat` | Records the installed application version number. |

**Behavior:**

1. **Fresh Installation**: Source folder contents are copied to the installation directory, and `installed_files.dat` and `installed_version.dat` are generated.

2. **Upgrade**:
   - Reads existing `installed_files.dat` to identify files installed by the previous version
   - Deletes files not included in the new version (user-created files are preserved)
   - Installs new files and updates `installed_files.dat`

3. **Uninstallation**:
   - Only deletes files recorded in `installed_files.dat`
   - Preserves user-created files (configs, logs, etc.)
   - Removes directories that become empty

**Note:** These management files are placed in the root of the installation folder and are automatically deleted during uninstallation.

---

### 4. Shortcuts

Configure shortcuts to create on desktop and start menu.

#### Desktop Shortcuts

Specify shortcuts to create on the user's desktop.

| Button | Description |
|--------|-------------|
| **+ File** | Add shortcut to an executable (.exe) |
| **+ Folder** | Add shortcut to a folder |
| **- Remove** | Remove selected shortcut |

#### Start Menu Shortcuts

Specify shortcuts to create in Windows Start Menu. A "Company Name" folder is created in the Start Menu containing these shortcuts (if company name is empty, the application name is used instead).

#### Shortcut Icons

| Type | Icon |
|------|------|
| **File** | Uses the target file's own icon |
| **Folder** | Uses Windows standard folder icon (shell32.dll) |

**Tips:**
- It's recommended to always add a shortcut to the main executable
- You can also add shortcuts to documentation folders or readme files

---

### 5. License

Configure the license agreement displayed during installation.

| Option | Description |
|--------|-------------|
| **Default License** | Use EasyNSIS's built-in default license text (Japanese/English based on language settings) |
| **External File** | Specify your own license file (.txt) |
| **Direct Input** | Enter license text directly |

**Supported Formats:**
- Plain text (.txt)

**Character Encoding Auto-Detection:**
- External file encoding is automatically detected using the UTF.Unknown library
- Supports major encodings including UTF-8, Shift_JIS, EUC-JP, etc.
- Automatically converts to system code page (CP932 for Japanese environments) for NSIS license display

---

### 6. Output Settings

Configure the output destination for the generated installer.

| Field | Description |
|-------|-------------|
| **Output Folder** | Folder where the installer (.exe) will be generated |

#### Output Files

The following files are generated in the output folder when building:

| File | Description |
|------|-------------|
| `<ApplicationName>_<Version>_Setup.exe` | Generated installer executable |
| `<ApplicationName>_Setup.nsi` | NSIS script file (for debugging/customization) |
| `<ApplicationName>_License.txt` | License text file (encoded in system code page) |

**Example:** If the application name is "MyApp" and version is "1.0.0.0":
- `MyApp_1.0.0.0_Setup.exe`
- `MyApp_Setup.nsi`
- `MyApp_License.txt`

---

### 7. Post-Install Settings

Configure actions to perform after installation completes.

| Option | Description |
|--------|-------------|
| **Open Install Folder** | Opens the installation folder in Explorer after installation |
| **Run a File** | Automatically runs the specified file after installation |

#### File Execution Settings

When "Run a File" is enabled, configure the following:

| Field | Description |
|-------|-------------|
| **File to Run** | Specify as a relative path from the source folder. Executable files (.exe) or batch files (.bat, .cmd) can be specified. |

**Use Cases:**
- Auto-launch the main application
- Run an initial setup wizard
- Display a setup completion message

---

### 8. Save/Load Configuration

Save and load current settings as XML files.

| Button | Description |
|--------|-------------|
| **Load** | Load a previously saved configuration file (.xml) |
| **Save** | Overwrite save current settings to existing file |
| **Save As** | Save current settings to a new file |

**Use Cases:**
- Save configuration files per project
- Share settings among team members
- Automated builds in CI/CD pipelines

---

## Building the Installer

Once all settings are complete, click the "**Build**" button at the bottom of the sidebar to generate the installer.

### During Build

- NSIS compiler output is displayed in real-time in the log area
- A spinner animation indicates processing
- Click "**Interrupt**" to cancel the build

### Build Complete

A result dialog appears when the build completes.

- **Success**: Click "Open Folder" to open the output location
- **Failure**: Check the log to identify the cause of the error

---

## Configuration File Format

Settings are saved in XML format.

```xml
<?xml version="1.0" encoding="utf-8"?>
<EasyNsisConfig version="1">
  <BasicInfo>
    <CompanyName>Company Name</CompanyName>
    <ApplicationName>App Name</ApplicationName>
    <Version>1.0.0.0</Version>
    <Language type="locale" value="en-US"/>
    <RegistrationMode>RegisterToAppsAndFeatures</RegistrationMode>
    <InstallerIcon>path/to/icon.ico</InstallerIcon>
  </BasicInfo>
  <SourceFolder>
    <Path>C:\source\folder</Path>
  </SourceFolder>
  <InstallDestination>
    <Type>AppDataRoaming</Type>
    <CustomPath></CustomPath>
    <AllowUserChange>true</AllowUserChange>
  </InstallDestination>
  <Shortcuts>
    <Desktop>
      <Item type="file">app.exe</Item>
    </Desktop>
    <StartMenu>
      <Item type="file">app.exe</Item>
    </StartMenu>
  </Shortcuts>
  <License>
    <Type>text</Type>
    <FilePath></FilePath>
    <Text></Text>
  </License>
  <Uninstall>
    <Cleanup>PreserveUserData</Cleanup>
  </Uninstall>
  <Output>
    <Folder>C:\output\folder</Folder>
  </Output>
  <PostInstall>
    <OpenInstallFolder>false</OpenInstallFolder>
    <RunAfterInstall>false</RunAfterInstall>
    <RunAfterInstallPath></RunAfterInstallPath>
  </PostInstall>
</EasyNsisConfig>
```

---

## Project Structure

```
EasyNSIS/
├── Assets/           # Application icons and default licenses
├── Licenses/         # Third-party license files (embedded)
├── Tools/nsis-3.11/  # NSIS compiler
├── Models/           # Data models
├── ViewModels/       # MVVM ViewModels
├── Views/            # Dialog windows
├── Services/         # Business logic services
├── Helpers/          # Value converters, theme helper
├── Resources/        # Localization resources
└── EasyNSIS.csproj
```

## Technology Stack

- **.NET 10.0** + **WPF** - Windows desktop application framework
- **Fluent Design** - Modern Windows UI theme (with Windows theme auto-detection)
- **CommunityToolkit.Mvvm** - MVVM pattern implementation
- **Microsoft.Extensions.DependencyInjection** - Dependency injection
- **UTF.Unknown** - Character encoding auto-detection library
- **NSIS 3.11** - Installer script compiler

### Windows Theme Support

EasyNSIS automatically detects the Windows theme setting (Light/Dark) and adjusts the UI colors accordingly.

- When you change the theme in Windows Settings → Personalization → Colors, the UI updates in real-time
- All cards, log area, buttons, and dialogs support theme switching
- The splitter between content area and log area is displayed with theme-appropriate colors for visibility

## Third-Party Licenses

This application uses the following open-source libraries:

| Library | License |
|---------|---------|
| [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) | MIT |
| [Microsoft.Extensions.DependencyInjection](https://github.com/dotnet/runtime) | MIT |
| [Microsoft.Xaml.Behaviors.Wpf](https://github.com/microsoft/XamlBehaviorsWpf) | MIT |
| [UTF.Unknown](https://github.com/CharsetDetector/UTF-unknown) | MIT / MPL 1.1 / LGPL |
| [NSIS](https://nsis.sourceforge.io/) | zlib/libpng |

See the **About / Licenses** dialog in the application for full license texts.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- [NSIS](https://nsis.sourceforge.io/) - Nullsoft Scriptable Install System
- [.NET Foundation](https://dotnetfoundation.org/) - .NET runtime and libraries
- [Microsoft](https://github.com/microsoft) - WPF and Fluent Design

## Author

kznagamori
