@setlocal
@echo off
SET DOTNET_VERSION=8.0
SET SANDBOX_FILES=C:\sandbox

set /p InstallMethod=<%SANDBOX_FILES%\dotnet.cfg

pushd "%TEMP%"
if "%InstallMethod%"=="EXE" goto EXE
if "%InstallMethod%"=="ZIP" goto ZIP
goto ERROR_NO_CONFIG

:ZIP
mkdir "%ProgramFiles%\dotnet"
if exist %SANDBOX_FILES%\dotnet-sdk.zip (
	tar -oxzf "%SANDBOX_FILES%\dotnet-sdk.zip" -C "%ProgramFiles%\dotnet"
) else (
	if %PROCESSOR_ARCHITECTURE%=="ARM64" (
		curl -L https://aka.ms/dotnet/%DOTNET_VERSION%/dotnet-sdk-win-arm64.zip --output dotnet-sdk.zip
	) else (
		curl -L https://aka.ms/dotnet/%DOTNET_VERSION%/dotnet-sdk-win-x64.zip --output dotnet-sdk.zip
	)
	if %errorlevel% NEQ 0 (
		echo "No pre-provided dotnet sdk, and failed to download.  Confirm networking is available."
		goto ERROR_NO_DOTNET
	)
	tar -oxzf dotnet-sdk.zip -C "%ProgramFiles%\dotnet"
	del dotnet-sdk.zip
)
goto PROCEED

:EXE
if exist %SANDBOX_FILES%\dotnet-sdk.exe (
	"%SANDBOX_FILES%\dotnet-sdk.exe" /install /quiet /norestart
) else (
	if %PROCESSOR_ARCHITECTURE%=="ARM64" (
		curl -L https://aka.ms/dotnet/%DOTNET_VERSION%/dotnet-sdk-win-arm64.exe --output dotnet-sdk.exe
	) else (
		curl -L https://aka.ms/dotnet/%DOTNET_VERSION%/dotnet-sdk-win-x64.exe --output dotnet-sdk.exe
	)
	if %errorlevel% NEQ 0 (
		echo "No pre-provided dotnet sdk, and failed to download.  Confirm networking is available."
		goto ERROR_NO_DOTNET
	)
	dotnet-sdk.exe /install /quiet /norestart
)
goto PROCEED

:PROCEED
endlocal
SETX PATH "%PATH%;%ProgramFiles%\dotnet" /M
SET PATH=%PATH%;%ProgramFiles%\dotnet

dotnet nuget locals all --clear
dotnet help

popd
cd c:\build
start "Menu" cmd /c C:\sandbox\runtest_menu.bat
goto END

:ERROR_NO_CONFIG
start "ERROR" CMD /c echo ERROR: Host configuration has not been run, run setup_sandbox.bat first ^& pause
goto END


:ERROR_NO_DOTNET
start "ERROR" CMD /c echo ERROR: Failed to find dotnet install, and download failed. Run setup_sandbox.bat again ^& pause

:END
