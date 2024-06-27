@setlocal enabledelayedexpansion
@echo off
SET DOTNET_VERSION=8.0

:MENU
cls
echo [0] Setup EXE install of DotNet for Sandbox
echo [1] Setup ZIP install of DotNet for Sandbox
echo [q] Quit
set /P "Option=Please select install option: "
if "%Option%"=="q" goto END
if "%Option%"=="0" goto EXE
if "%Option%"=="1" goto ZIP

:MENUERROR
cls
echo ERROR: Invalid Option Selected!!
pause
goto MENU

:EXE
echo EXE> dotnet.cfg
if not exist .\assets mkdir .\assets
if %PROCESSOR_ARCHITECTURE%=="ARM64" (
	curl -L0 https://aka.ms/dotnet/%DOTNET_VERSION%/dotnet-sdk-win-arm64.exe --output ".\assets\dotnet-sdk-64.exe"
	curl -L0 https://aka.ms/dotnet/%DOTNET_VERSION%/windowsdesktop-runtime-win-arm64.exe --output ".\assets\windowsdesktop-runtime-64.exe"
) else (
	curl -L0 https://aka.ms/dotnet/%DOTNET_VERSION%/dotnet-sdk-win-x64.exe --output ".\assets\dotnet-sdk-64.exe"
	curl -L0 https://aka.ms/dotnet/%DOTNET_VERSION%/windowsdesktop-runtime-win-x86.exe --output ".\assets\windowsdesktop-runtime-x86.exe"
	curl -L0 https://aka.ms/dotnet/%DOTNET_VERSION%/windowsdesktop-runtime-win-x64.exe --output ".\assets\windowsdesktop-runtime-64.exe"
)
goto VSDEBUG

:ZIP
echo ZIP> dotnet.cfg
if not exist .\assets mkdir .\assets
if %PROCESSOR_ARCHITECTURE%=="ARM64" (
	curl -L0 https://aka.ms/dotnet/%DOTNET_VERSION%/dotnet-sdk-win-arm64.zip --output ".\assets\dotnet-sdk-64.zip"
	curl -L0 https://aka.ms/dotnet/%DOTNET_VERSION%/windowsdesktop-runtime-win-arm64.zip --output ".\assets\windowsdesktop-runtime-64.zip"
) else (
	curl -L0 https://aka.ms/dotnet/%DOTNET_VERSION%/dotnet-sdk-win-x64.zip --output ".\assets\dotnet-sdk-64.zip"
	curl -L0 https://aka.ms/dotnet/%DOTNET_VERSION%/windowsdesktop-runtime-win-x64.zip --output ".\assets\windowsdesktop-runtime-64.zip"
	curl -L0 https://aka.ms/dotnet/%DOTNET_VERSION%/dotnet-runtime-win-x86.zip --output ".\assets\dotnet-runtime-x86.zip"
	curl -L0 https://aka.ms/dotnet/%DOTNET_VERSION%/windowsdesktop-runtime-win-x86.zip --output ".\assets\windowsdesktop-runtime-x86.zip"
)
goto VSDEBUG

:VSDEBUG
for /f "usebackq tokens=*" %%i in (`"C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe" -latest -requires Microsoft.VisualStudio.Debugger.Remote -property installationPath`) do (
  set VsInstallDir=%%i
)
if "!VsInstallDir!"=="" (
	for /f "usebackq tokens=*" %%i in (`"C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe" -prerelease -latest -requires Microsoft.VisualStudio.Debugger.Remote -property installationPath`) do (
	  set VsInstallDir=%%i
	)
)
if not "!VsInstallDir!"=="" (
	echo.
	echo Have found VisualStudio Debugger at '%VsInstallDir%'
	set /P "Confirm=Do you wish to copy it for use by the Sandbox? (Y / N):"
	if "!Confirm!"=="Y" goto VSDEBUG_COPY
	if "!Confirm!"=="y" goto VSDEBUG_COPY
	goto END
)
goto END

:VSDEBUG_COPY
if not exist ".\assets\Debugger" (mkdir ".\assets\Debugger")
XCOPY "%VsInstallDir%\Common7\IDE\Remote Debugger\*" ".\assets\Debugger\" /E /Y > nul
echo Debugger files copied


:END
pause
@endlocal
