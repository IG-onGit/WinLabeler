# WinSpace

**Set up your whole workspace with one click.** WinSpace is a tiny Windows tray
app that turns each virtual desktop into a ready-to-go workspace: pick a
workplace and it launches your editor, browser and tools, then labels and
colors the desktop so you always know where you are.

## Features

- **Workplace launcher** - a workplace is a label, a color and a list of
  commands (one per line, run through `cmd.exe`). One click starts everything
- **Automated setup** - the desktop is labeled and colored to match the
  workplace you launched
- **Built-in workplace manager** - add, edit and delete workplaces without touching files
- One label per virtual desktop, each with its own name, color and screen corner
- Double-click to rename, drag to move (snaps to the nearest corner)
- 11 preset background colors plus a custom color picker
- Optional "Always on top" and "Start with Windows"
- Local and private: settings saved in `%AppData%\WinSpace\settings.json`, no network code

## Requirements

- Windows 10 or 11, 64-bit (developed and tested on Windows 10 LTSC 1809)
- [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) (x64)
  to run the released exe
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) to build from source

> The app finds the current desktop by reading undocumented registry values
> that Microsoft may change between Windows versions. If labels don't switch
> correctly on your version, please open an issue.

## Install

**From a release:** download `WinSpace.exe` from the
[Releases](../../releases) page, put it in a permanent folder such as
`C:\Tools\WinSpace`, and run it. If SmartScreen warns you, click
**More info -> Run anyway** (the exe is not code-signed).

**From source:**

```
winget install Microsoft.DotNet.SDK.10
git clone <this repository>
cd <repository folder>
build.bat
```

The exe is created at `publish\WinSpace.exe`. For a build that runs
without the .NET runtime installed, change `--self-contained false` to
`--self-contained true` in `build.bat` (the file will be much larger).

## Usage

| Action | How |
|---|---|
| Launch a workplace | Click the label, then pick a workplace |
| Add or edit workplaces | Click the label -> **Manage** (label, commands, color) |
| Rename the label | Double-click it, type the name, press Enter (Esc cancels) |
| Move the label | Drag it - it snaps to the nearest screen corner |
| Change the color | Right-click -> **Background color** (presets or **Custom...**) |
| Pick a corner | Right-click -> **Move to corner** |
| Keep above windows | Right-click -> **Always on top** |
| Start with Windows | Right-click -> **Start with Windows** |
| Quit | Right-click the label or the tray icon -> **Exit** |

"Start with Windows" adds a `WinSpace` entry under
`HKCU\Software\Microsoft\Windows\CurrentVersion\Run` that points to the exe's
current path, so don't move the exe afterwards (or toggle the option off and on
again after moving it).

## Uninstall

1. Right-click the label and untick **Start with Windows**.
2. Right-click -> **Exit**.
3. Delete the exe. Optionally delete `%AppData%\WinSpace` to remove settings.

## Troubleshooting

- **`build.bat` says ".NET SDK was not found"** - install the SDK, then reopen your terminal.
- **Build error about the target framework** - your SDK is older; change
  `net10.0-windows` in `WinSpace.csproj` to match it (e.g. `net8.0-windows`).
- **I don't see the label** - check the corner above the taskbar and the tray icon.
  With "Always on top" off, windows can cover the label.
- **A label shows just its number after renaming** - you saved an empty name, which means "use the default".

## License

[MIT](LICENSE)
