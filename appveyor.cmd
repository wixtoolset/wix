@setlocal
@pushd %~dp0
@set _P=%~dp0build\Release\publish

dotnet pack -c Release src\WixToolset.Converters || exit /b
dotnet pack -c Release src\WixToolset.Converters.Tupleizer || exit /b

dotnet build -c Release src\test\WixToolsetTest.Converters || exit /b
dotnet build -c Release src\test\WixToolsetTest.Converters.Tupleizer || exit /b

@popd
@endlocal
