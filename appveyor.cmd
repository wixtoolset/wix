@setlocal
@pushd %~dp0
@set _P=%~dp0build\Release\publish

@rem Disable this test until publishing of native assets is worked out
@rem dotnet build -c Release src\test\WixToolsetTest.BuildTasks

dotnet publish -c Release -o %_P%\dotnet-wix\ -f netcoreapp2.1 src\wix
dotnet publish -c Release -o %_P%\WixToolset.MSBuild\net461\ -f net461 src\WixToolset.BuildTasks
dotnet publish -c Release -o %_P%\WixToolset.MSBuild\netcoreapp2.1\ -f netcoreapp2.1 src\WixToolset.BuildTasks

@rem dotnet publish -c Release -o %_P%\netcoreapp2.1 -r win-x86 src\wix
@rem dotnet publish -c Release -o %_P%\net461 -r win-x86 src\light
@rem dotnet publish -c Release -o %_P%\net461 -r win-x86 src\WixToolset.BuildTasks

dotnet pack -c Release src\dotnet-wix
dotnet pack -c Release src\WixToolset.MSBuild
@rem dotnet pack -c Release src\WixToolset.Core.InternalPackage

@popd
@endlocal
