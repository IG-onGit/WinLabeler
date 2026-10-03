# WinLabeler

A tiny Windows tray app that shows a small rounded label (for example "Work" or
"Email") on each of your virtual desktops, so you always know which desktop you
are on. Every desktop has its own name, color and screen corner.

## Features

- One label that follows you and shows the current desktop's name
- Double-click to rename, drag to move (snaps to the nearest corner)
- 10 preset background colors plus a custom color picker
- Optional "Always on top" and "Start with Windows"
- Settings saved per desktop in `%AppData%\WinLabeler\settings.json`

## Requirements

- Windows 10 or 11, 64-bit (developed and tested on Windows 10 LTSC 1809)
- [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) (x64)
  to run the released exe
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) to build from source

> The app finds the current desktop by reading undocumented registry values
> that Microsoft may change between Windows versions. If labels don't switch
> correctly on your version, please open an issue.

## Install

**From a release:** download `WinLabeler.exe` from the
[Releases](../../releases) page, put it in a permanent folder such as
`C:\Tools\WinLabeler`, and run it. If SmartScreen warns you, click
**More info -> Run anyway** (the exe is not code-signed).

**From source:**

```
winget install Microsoft.DotNet.SDK.10
git clone <this repository>
cd <repository folder>
build.bat
```

The exe is created at `publish\WinLabeler.exe`. For a build that runs
without the .NET runtime installed, change `--self-contained false` to
`--self-contained true` in `build.bat` (the file will be much larger).

## Usage

| Action | How |
|---|---|
| Rename the label | Double-click it, type the name, press Enter (Esc cancels) |
| Move the label | Drag it - it snaps to the nearest screen corner |
| Change the color | Right-click -> **Background color** (presets or **Custom...**) |
| Pick a corner | Right-click -> **Move to corner** |
| Keep above windows | Right-click -> **Always on top** |
| Start with Windows | Right-click -> **Start with Windows** |
| Quit | Right-click the label or the tray icon -> **Exit** |

"Start with Windows" adds a `WinLabeler` entry under
`HKCU\Software\Microsoft\Windows\CurrentVersion\Run` that points to the exe's
current path, so don't move the exe afterwards (or toggle the option off and on
again after moving it).

## Uninstall

1. Right-click the label and untick **Start with Windows**.
2. Right-click -> **Exit WinLabeler**.
3. Delete the exe. Optionally delete `%AppData%\WinLabeler` to remove settings.

## Troubleshooting

- **`build.bat` says ".NET SDK was not found"** - install the SDK, then reopen your terminal.
- **Build error about the target framework** - your SDK is older; change
  `net10.0-windows` in `WinLabeler.csproj` to match it (e.g. `net8.0-windows`).
- **I don't see the label** - check the corner above the taskbar and the tray icon.
  With "Always on top" off, windows can cover the label.
- **A label shows just its number after renaming** - you saved an empty name, which means "use the default".

## License

[MIT](LICENSE)
