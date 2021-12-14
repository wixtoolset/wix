@echo off
setlocal enabledelayedexpansion

echo Configuring Visual Studio to build WiX Toolset...

for /f "usebackq tokens=*" %%i in (`"C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe" -latest -property installationPath`) do (
  echo Visual Studio installed at: "%%i"
  "C:\Program Files (x86)\Microsoft Visual Studio\Installer\vs_installer.exe" modify --config "%~dp0wix.vsconfig" --installPath "%%i" --quiet --norestart --force
  echo Configuration complete, exit code: !errorlevel!
  exit /b !errorlevel!
)
