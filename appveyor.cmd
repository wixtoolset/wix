@setlocal
@pushd %~dp0

dotnet build -c Release
dotnet pack -c Release

@popd
@endlocal