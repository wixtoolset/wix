@setlocal
@pushd %~dp0

dotnet pack -c Release

@popd
@endlocal