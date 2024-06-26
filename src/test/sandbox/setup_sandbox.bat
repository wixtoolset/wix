@setlocal
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
if %PROCESSOR_ARCHITECTURE%=="ARM64" (
	curl -L0 https://aka.ms/dotnet/%DOTNET_VERSION%/dotnet-sdk-win-arm64.exe --output ".\dotnet-sdk.exe"
) else (
	curl -L0 https://aka.ms/dotnet/%DOTNET_VERSION%/dotnet-sdk-win-x64.exe --output ".\dotnet-sdk.exe"
)
goto VSDEBUG

:ZIP
echo ZIP> dotnet.cfg
if %PROCESSOR_ARCHITECTURE%=="ARM64" (
	curl -L0 https://aka.ms/dotnet/%DOTNET_VERSION%/dotnet-sdk-win-arm64.zip --output ".\dotnet-sdk.zip"
) else (
	curl -L0 https://aka.ms/dotnet/%DOTNET_VERSION%/dotnet-sdk-win-x64.zip --output ".\dotnet-sdk.zip"
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
	if "%Confirm%"=="Y" goto VSDEBUG_COPY
	if "%Confirm%"=="y" goto VSDEBUG_COPY
	goto END
)
goto END

:VSDEBUG_COPY
if not exist Debugger (mkdir Debugger)
XCOPY "%VsInstallDir%\Common7\IDE\Remote Debugger\*" ".\Debugger\" /E /Y > nul
echo Debugger files copied


:END
pause
@endlocal
