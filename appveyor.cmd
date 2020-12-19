@setlocal
@pushd %~dp0
@set _P=%~dp0build\Release\publish

dotnet test -c Release src\test\WixToolsetTest.Converters || exit /b
dotnet test -c Release src\test\WixToolsetTest.Converters.Symbolizer || exit /b

dotnet pack -c Release src\WixToolset.Converters || exit /b
dotnet pack -c Release src\WixToolset.Converters.Symbolizer || exit /b

@popd
@endlocal
