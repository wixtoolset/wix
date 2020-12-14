@setlocal
@pushd %~dp0
@set _P=%~dp0build\Release\publish

dotnet test -c Release src\test\WixToolsetTest.CoreIntegration || exit /b
dotnet test -c Release src\test\WixToolsetTest.Core.Burn || exit /b

dotnet pack -c Release src\WixToolset.Core || exit /b
dotnet pack -c Release src\WixToolset.Core.Burn || exit /b
dotnet pack -c Release src\WixToolset.Core.ExtensionCache || exit /b
dotnet pack -c Release src\WixToolset.Core.WindowsInstaller || exit /b

dotnet pack -c Release src\WixToolset.Core.TestPackage || exit /b

@popd
@endlocal
