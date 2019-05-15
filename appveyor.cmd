@setlocal
@pushd %~dp0
@set _P=%~dp0build\Release\publish

dotnet pack -c Release src\WixToolset.Converters
dotnet pack -c Release src\WixToolset.Converters.Tupleizer

dotnet build -c Release src\test\WixToolsetTest.Converters
dotnet build -c Release src\test\WixToolsetTest.Converters.Tupleizer

@popd
@endlocal
