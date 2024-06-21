@setlocal
SET DOTNET_VERSION=8.0
SET SANDBOX_FILES=C:\sandbox

pushd "%TEMP%"

mkdir "%ProgramFiles%\dotnet"
@if exist %SANDBOX_FILES%\%PROCESSOR_ARCHITECTURE%\dotnet-runtime.zip (
	tar -oxzf "%SANDBOX_FILES%\%PROCESSOR_ARCHITECTURE%\dotnet-runtime.zip" -C "%ProgramFiles%\dotnet"
) else (
	if %PROCESSOR_ARCHITECTURE%=="ARM64" (
		curl -L https://aka.ms/dotnet/%DOTNET_VERSION%/dotnet-runtime-win-arm64.zip --output dotnet-runtime.zip
	) else (
		curl -L https://aka.ms/dotnet/%DOTNET_VERSION%/dotnet-runtime-win-x64.zip --output dotnet-runtime.zip
	)
	if %errorlevel$ NEQ 0 (
	echo No pre-provided dotnet runtime, and failed to download.  Confirm networking is available.
	goto :ERROR
	)
	tar -oxzf dotnet-runtime.zip -C "%ProgramFiles%\dotnet"
	del dotnet-runtime.zip
)

@if exist %SANDBOX_FILES%\%PROCESSOR_ARCHITECTURE%\dotnet-sdk.zip (
	tar -oxzf "%SANDBOX_FILES%\%PROCESSOR_ARCHITECTURE%\dotnet-sdk.zip" -C "%ProgramFiles%\dotnet"
) else (
	if %PROCESSOR_ARCHITECTURE%=="ARM64" (
		curl -L https://aka.ms/dotnet/%DOTNET_VERSION%/dotnet-sdk-win-arm64.zip --output dotnet-sdk.zip
	) else (
		curl -L https://aka.ms/dotnet/%DOTNET_VERSION%/dotnet-sdk-win-x64.zip --output dotnet-runtime.zip
	)
	if %errorlevel$ NEQ 0 echo "No pre-provided dotnet sdk, and failed to download.  Confirm networking is available." goto exit
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
