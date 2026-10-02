@echo off
cd /d "%~dp0"

where dotnet >nul 2>nul
if errorlevel 1 (
    echo .NET SDK was not found. Please install it first - see README.md, Step 1.
    pause
    exit /b 1
)

echo Building WinLabeler...
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o "%~dp0publish"
if errorlevel 1 (
    echo.
    echo Build failed. See the messages above.
    pause
    exit /b 1
)

echo.
echo Done! Your program is here:
echo %~dp0publish\WinLabeler.exe
explorer "%~dp0publish"
pause
