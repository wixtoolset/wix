@setlocal
SET DOTNET_VERSION=8.0
SET SANDBOX_FILES=C:\sandbox

pushd "%TEMP%"

mkdir "%ProgramFiles%\dotnet"
REM @if exist %SANDBOX_FILES%\%PROCESSOR_ARCHITECTURE%\dotnet-runtime.zip (
REM 	tar -oxzf "%SANDBOX_FILES%\%PROCESSOR_ARCHITECTURE%\dotnet-runtime.zip" -C "%ProgramFiles%\dotnet"
REM ) else (
REM 	if %PROCESSOR_ARCHITECTURE%=="ARM64" (
REM 		curl -L https://aka.ms/dotnet/%DOTNET_VERSION%/dotnet-runtime-win-arm64.zip --output dotnet-runtime.zip
REM 	) else (
REM 		curl -L https://aka.ms/dotnet/%DOTNET_VERSION%/dotnet-runtime-win-x64.zip --output dotnet-runtime.zip
REM 	)
REM 	if %errorlevel% NEQ 0 (
REM 	echo No pre-provided dotnet runtime, and failed to download.  Confirm networking is available.
REM 	goto :ERROR
REM 	)
REM 	tar -oxzf dotnet-runtime.zip -C "%ProgramFiles%\dotnet"
REM 	del dotnet-runtime.zip
REM )

@if exist %SANDBOX_FILES%\%PROCESSOR_ARCHITECTURE%\dotnet-sdk.zip (
	tar -oxzf "%SANDBOX_FILES%\%PROCESSOR_ARCHITECTURE%\dotnet-sdk.zip" -C "%ProgramFiles%\dotnet"
) else (
	if %PROCESSOR_ARCHITECTURE%=="ARM64" (
		curl -L https://aka.ms/dotnet/%DOTNET_VERSION%/dotnet-sdk-win-arm64.zip --output dotnet-sdk.zip
	) else (
		curl -L https://aka.ms/dotnet/%DOTNET_VERSION%/dotnet-sdk-win-x64.zip --output dotnet-sdk.zip
	)
	if %errorlevel% NEQ 0 (
		echo "No pre-provided dotnet sdk, and failed to download.  Confirm networking is available."
		goto ERROR
	)
	tar -oxzf dotnet-sdk.zip -C "%ProgramFiles%\dotnet"
	del dotnet-sdk.zip
)

@endlocal
SETX PATH "%PATH%;%ProgramFiles%\dotnet" /M
SET PATH=%PATH%;%ProgramFiles%\dotnet
dotnet nuget locals all --clear
dotnet help


:ERROR

@popd
cd c:\build
start cmd /c C:\sandbox\runtest_menu.bat
