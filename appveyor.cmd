@setlocal
@pushd %~dp0
@set _P=%~dp0build\Release\publish

dotnet build -c Release src\test\WixToolsetTest.BuildTasks
dotnet build -c Release src\test\WixToolsetTest.CoreIntegration
dotnet build -c Release src\test\WixToolsetTest.LightIntegration

dotnet publish -c Release -o %_P%\netcoreapp2.0 -r win-x86 src\wix
dotnet publish -c Release -o %_P%\net461 -r win-x86 src\light
dotnet publish -c Release -o %_P%\net461 -r win-x86 src\WixToolset.BuildTasks

dotnet pack -c Release src\WixToolset.Core.InternalPackage

@popd
@endlocal
