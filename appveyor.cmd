@setlocal
@pushd %~dp0
@set _P=%~dp0build\Release\publish

nuget restore

dotnet test -c Release src\test\WixToolsetTest.BuildTasks
dotnet test -c Release src\test\WixToolsetTest.WixCop

dotnet publish -c Release -o %_P%\dotnet-wix\ -f netcoreapp2.1 src\wix
dotnet publish -c Release -o %_P%\WixToolset.MSBuild\net461\ -f net461 src\WixToolset.BuildTasks
dotnet publish -c Release -o %_P%\WixToolset.MSBuild\net472\ -f net472 src\WixToolset.BuildTasks
dotnet publish -c Release -o %_P%\WixToolset.MSBuild\netstandard2.0\ -f netstandard2.0 src\WixToolset.BuildTasks

@rem dotnet publish -c Release -o %_P%\netcoreapp2.1 -r win-x86 src\wix
@rem dotnet publish -c Release -o %_P%\net461 -r win-x86 src\light
@rem dotnet publish -c Release -o %_P%\net461 -r win-x86 src\WixToolset.BuildTasks

dotnet pack -c Release src\dotnet-wix
dotnet pack -c Release src\WixToolset.MSBuild

msbuild -p:Configuration=Release .\src\ThmViewerPackage\ThmViewerPackage.wixproj

@popd
@endlocal
