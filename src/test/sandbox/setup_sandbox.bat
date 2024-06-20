@setlocal
@SET DOTNET_VERSION=8.0

@if not exist AMD64 (mkdir AMD64)
@if not exist ARM64 (mkdir ARM64)
@REM if not exist VSTest (mkdir VSTest)

curl -L0 https://aka.ms/dotnet/%DOTNET_VERSION%/dotnet-runtime-win-x64.zip --output ".\AMD64\dotnet-runtime.zip"
curl -L0 https://aka.ms/dotnet/%DOTNET_VERSION%/dotnet-sdk-win-x64.zip --output ".\AMD64\dotnet-sdk.zip"
curl -L0 https://aka.ms/dotnet/%DOTNET_VERSION%/dotnet-runtime-win-arm64.zip --output ".\ARM64\dotnet-runtime.zip"
curl -L0 https://aka.ms/dotnet/%DOTNET_VERSION%/dotnet-sdk-win-arm64.zip --output ".\ARM64\dotnet-sdk.zip"

REM "%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" -prerelease


@endlocal
