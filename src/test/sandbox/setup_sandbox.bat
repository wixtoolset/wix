@setlocal
@echo off
SET DOTNET_VERSION=8.0

if not exist AMD64 (mkdir AMD64)
if not exist ARM64 (mkdir ARM64)
REM if not exist VSTest (mkdir VSTest)

@echo on
curl -L0 https://aka.ms/dotnet/%DOTNET_VERSION%/dotnet-runtime-win-x64.zip --output ".\AMD64\dotnet-runtime.zip"
curl -L0 https://aka.ms/dotnet/%DOTNET_VERSION%/dotnet-sdk-win-x64.zip --output ".\AMD64\dotnet-sdk.zip"
curl -L0 https://aka.ms/dotnet/%DOTNET_VERSION%/dotnet-runtime-win-arm64.zip --output ".\ARM64\dotnet-runtime.zip"
curl -L0 https://aka.ms/dotnet/%DOTNET_VERSION%/dotnet-sdk-win-arm64.zip --output ".\ARM64\dotnet-sdk.zip"
@echo off

REM curl -L0 https://aka.ms/vs/17/release/RemoteTools.amd64ret.enu.exe --output ".\AMD64\RemoteTools.exe"
REM curl -L0 https://aka.ms/vs/17/release/RemoteTools.arm64ret.enu.exe --output ".\ARM64\RemoteTools.exe"

for /f "usebackq tokens=*" %%i in (`"C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe" -latest -requires Microsoft.VisualStudio.Debugger.Remote -property installationPath`) do (
  set VsInstallDir=%%i
)
if "!VsInstallDir!"=="" (
	for /f "usebackq tokens=*" %%i in (`"C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe" -prerelease -latest -requires Microsoft.VisualStudio.Debugger.Remote -property installationPath`) do (
	  set VsInstallDir=%%i
	)
)
if "!VsInstallDir!"=="" (
	set /P "Confirm=Have found VisualStudio Debugger at '%VsInstallDir%', Do you wish to copy it for use by the Sandbox? (Y / N):"
	if "%Confirm%"=="Y" (
		XCOPY "%VsInstallDir%\Common7\IDE\Remote Debugger\*" ".\Debugger\" /E
	)
)


pause
@endlocal
