@setlocal
@pushd %~dp0
@set _C=Release
@set _P=%~dp0build\%_C%\publish
@set _RCO=/S /R:1 /W:1 /NP /XO

nuget restore

dotnet test -c %_C% src\test\WixToolsetTest.BuildTasks
dotnet test -c %_C% src\test\WixToolsetTest.WixCop

dotnet publish -c %_C% -o %_P%\dotnet-wix\ -f netcoreapp2.1 src\wix

dotnet publish -c %_C% -o %_P%\WixToolset.MSBuild\separate\net461\x86\buildtasks\ -f net461 -r win-x86 src\WixToolset.BuildTasks
dotnet publish -c %_C% -o %_P%\WixToolset.MSBuild\separate\net461\x86\heat\ -f net461 -r win-x86 src\heat
dotnet publish -c %_C% -o %_P%\WixToolset.MSBuild\separate\net461\x86\wix\ -f net461 -r win-x86 src\wix
dotnet publish -c %_C% -o %_P%\WixToolset.MSBuild\separate\net461\x86\wixcop\ -f net461 -r win-x86 src\wixcop
robocopy %_P%\WixToolset.MSBuild\separate\net461\x86\buildtasks %_P%\WixToolset.MSBuild\tools\net461\x86 %_RCO%
robocopy %_P%\WixToolset.MSBuild\separate\net461\x86\heat %_P%\WixToolset.MSBuild\tools\net461\x86 %_RCO%
robocopy %_P%\WixToolset.MSBuild\separate\net461\x86\wix %_P%\WixToolset.MSBuild\tools\net461\x86 %_RCO%
robocopy %_P%\WixToolset.MSBuild\separate\net461\x86\wixcop %_P%\WixToolset.MSBuild\tools\net461\x86 %_RCO%

dotnet publish -c %_C% -o %_P%\WixToolset.MSBuild\separate\net461\x64\buildtasks\ -f net461 -r win-x64 src\WixToolset.BuildTasks
dotnet publish -c %_C% -o %_P%\WixToolset.MSBuild\separate\net461\x64\heat\ -f net461 -r win-x64 src\heat
dotnet publish -c %_C% -o %_P%\WixToolset.MSBuild\separate\net461\x64\wix\ -f net461 -r win-x64 src\wix
dotnet publish -c %_C% -o %_P%\WixToolset.MSBuild\separate\net461\x64\wixcop\ -f net461 -r win-x64 src\wixcop
robocopy %_P%\WixToolset.MSBuild\separate\net461\x64\buildtasks %_P%\WixToolset.MSBuild\tools\net461\x64 %_RCO%
robocopy %_P%\WixToolset.MSBuild\separate\net461\x64\heat %_P%\WixToolset.MSBuild\tools\net461\x64 %_RCO%
robocopy %_P%\WixToolset.MSBuild\separate\net461\x64\wix %_P%\WixToolset.MSBuild\tools\net461\x64 %_RCO%
robocopy %_P%\WixToolset.MSBuild\separate\net461\x64\wixcop %_P%\WixToolset.MSBuild\tools\net461\x64 %_RCO%

dotnet publish -c %_C% -o %_P%\WixToolset.MSBuild\separate\netcoreapp2.1\buildtasks\ -f netcoreapp2.1 src\WixToolset.BuildTasks
dotnet publish -c %_C% -o %_P%\WixToolset.MSBuild\separate\netcoreapp2.1\heat\ -f netcoreapp2.1 src\heat
dotnet publish -c %_C% -o %_P%\WixToolset.MSBuild\separate\netcoreapp2.1\wix\ -f netcoreapp2.1 src\wix
dotnet publish -c %_C% -o %_P%\WixToolset.MSBuild\separate\netcoreapp2.1\wixcop\ -f netcoreapp2.1 src\wixcop
robocopy %_P%\WixToolset.MSBuild\separate\netcoreapp2.1\buildtasks %_P%\WixToolset.MSBuild\tools\netcoreapp2.1 %_RCO%
robocopy %_P%\WixToolset.MSBuild\separate\netcoreapp2.1\heat %_P%\WixToolset.MSBuild\tools\netcoreapp2.1 %_RCO%
robocopy %_P%\WixToolset.MSBuild\separate\netcoreapp2.1\wix %_P%\WixToolset.MSBuild\tools\netcoreapp2.1 %_RCO%
robocopy %_P%\WixToolset.MSBuild\separate\netcoreapp2.1\wixcop %_P%\WixToolset.MSBuild\tools\netcoreapp2.1 %_RCO%

dotnet publish -c %_C% -o %_P%\WixToolset.MSBuild\ src\WixToolset.MSBuild
dotnet publish -c %_C% -o %_P%\WixToolset.MSBuild\broken\net461\ -f net461 -r dne src\wix

dotnet test -c %_C% src\test\WixToolsetTest.MSBuild

dotnet pack -c %_C% src\dotnet-wix
dotnet pack -c %_C% src\WixToolset.MSBuild

msbuild -p:Configuration=%_C% .\src\ThmViewerPackage\ThmViewerPackage.wixproj

@popd
@endlocal
