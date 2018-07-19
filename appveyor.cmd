@setlocal
@pushd %~dp0
@set _P=%~dp0build\Release\publish

dotnet pack -c Release src\WixToolset.Core
dotnet pack -c Release src\WixToolset.Core.Burn
dotnet pack -c Release src\WixToolset.Core.WindowsInstaller

dotnet pack -c Release src\WixToolset.Core.TestPackage

dotnet build -c Release src\test\WixToolsetTest.CoreIntegration

@popd
@endlocal
