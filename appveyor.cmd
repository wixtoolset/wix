@setlocal
@pushd %~dp0
@set _C=Release
@set _P=%~dp0build\%_C%\publish

nuget restore

dotnet test -c %_C% src\test\WixToolsetTest.BuildTasks
dotnet test -c %_C% src\test\WixToolsetTest.WixCop

dotnet publish -c %_C% -o %_P%\dotnet-wix\ -f netcoreapp2.1 src\wix

dotnet publish -c %_C% -o %_P%\WixToolset.MSBuild\tools\net461\x86\ -f net461 -r win-x86 src\WixToolset.BuildTasks
dotnet publish -c %_C% -o %_P%\WixToolset.MSBuild\tools\net461\x86\ -f net461 -r win-x86 src\heat
dotnet publish -c %_C% -o %_P%\WixToolset.MSBuild\tools\net461\x86\ -f net461 -r win-x86 src\wix
dotnet publish -c %_C% -o %_P%\WixToolset.MSBuild\tools\net461\x86\ -f net461 -r win-x86 src\wixcop
dotnet publish -c %_C% -o %_P%\WixToolset.MSBuild\tools\net461\x64\ -f net461 -r win-x64 src\WixToolset.BuildTasks
dotnet publish -c %_C% -o %_P%\WixToolset.MSBuild\tools\net461\x64\ -f net461 -r win-x64 src\heat
dotnet publish -c %_C% -o %_P%\WixToolset.MSBuild\tools\net461\x64\ -f net461 -r win-x64 src\wix
dotnet publish -c %_C% -o %_P%\WixToolset.MSBuild\tools\net461\x64\ -f net461 -r win-x64 src\wixcop
dotnet publish -c %_C% -o %_P%\WixToolset.MSBuild\tools\netcoreapp2.1\ -f netcoreapp2.1 src\WixToolset.BuildTasks
dotnet publish -c %_C% -o %_P%\WixToolset.MSBuild\tools\netcoreapp2.1\ -f netcoreapp2.1 src\heat
dotnet publish -c %_C% -o %_P%\WixToolset.MSBuild\tools\netcoreapp2.1\ -f netcoreapp2.1 src\wix
dotnet publish -c %_C% -o %_P%\WixToolset.MSBuild\tools\netcoreapp2.1\ -f netcoreapp2.1 src\wixcop
dotnet publish -c %_C% -o %_P%\WixToolset.MSBuild\ src\WixToolset.MSBuild
dotnet publish -c %_C% -o %_P%\WixToolset.MSBuild\broken\net461\ -f net461 -r dne src\wix

dotnet pack -c %_C% src\dotnet-wix
dotnet pack -c %_C% src\WixToolset.MSBuild

dotnet test -c %_C% src\test\WixToolsetTest.MSBuild

msbuild -p:Configuration=%_C% .\src\ThmViewerPackage\ThmViewerPackage.wixproj

@popd
@endlocal
