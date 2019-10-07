@setlocal
@pushd %~dp0

dotnet test -c Release src\test\WixToolsetTest.Data\WixToolsetTest.Data.csproj
dotnet pack -c Release

@popd
@endlocal