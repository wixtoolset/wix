@setlocal
@pushd %~dp0
@set _P=%~dp0build\Release\publish

dotnet test -c Release src\test\WixToolsetTest.CoreIntegration

dotnet pack -c Release src\WixToolset.Core
dotnet pack -c Release src\WixToolset.Core.Burn
dotnet pack -c Release src\WixToolset.Core.WindowsInstaller

dotnet pack -c Release src\WixToolset.Core.TestPackage

@popd
@endlocal
