@setlocal
@pushd %~dp0

dotnet test -c Release src\test\WixToolsetTest.Data\WixToolsetTest.Data.csproj || exit /b
dotnet pack -c Release || exit /b

@popd
@endlocal