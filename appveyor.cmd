@setlocal
@pushd %~dp0
@set _C=Release
@set _P=%~dp0build\%_C%\publish

nuget restore

dotnet test -c %_C% src\test\WixToolsetTest.BuildTasks
dotnet test -c %_C% src\test\WixToolsetTest.WixCop

dotnet publish -c %_C% -o %_P%\dotnet-wix\ -f netcoreapp2.1 src\wix
@rem dotnet publish -c %_C% -o %_P%\netfx-heat\ -f net461 src\heat
@rem dotnet publish -c %_C% -o %_P%\netfx-wix\ -f net461 src\wix
@rem dotnet publish -c %_C% -o %_P%\netfx-wixcop\ -f net461 src\wixcop
dotnet publish -c %_C% -o %_P%\WixToolset.MSBuild\tools\net461\x86\ -f net461 -r win-x86 src\WixToolset.BuildTasks
dotnet publish -c %_C% -o %_P%\WixToolset.MSBuild\tools\net461\x64\ -f net461 -r win-x64 src\WixToolset.BuildTasks
dotnet publish -c %_C% -o %_P%\WixToolset.MSBuild\tools\netcoreapp2.1\ -f netcoreapp2.1 src\WixToolset.BuildTasks
dotnet publish -c %_C% -o %_P%\WixToolset.MSBuild\ src\WixToolset.MSBuild

dotnet pack -c %_C% src\dotnet-wix
dotnet pack -c %_C% src\WixToolset.MSBuild

dotnet test -c %_C% src\test\WixToolsetTest.MSBuild

msbuild -p:Configuration=%_C% .\src\ThmViewerPackage\ThmViewerPackage.wixproj

@popd
@endlocal
