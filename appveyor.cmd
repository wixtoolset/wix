@setlocal
@pushd %~dp0
@set _C=Release
@set _P=%~dp0build\%_C%\publish
@set _RCO=/S /R:1 /W:1 /NP /XO  /NS /NC /NFL /NDL /NJH /NJS

nuget restore || exit /b %ERRORLEVEL%

dotnet test -c %_C% src\test\WixToolsetTest.BuildTasks || exit /b %ERRORLEVEL%

dotnet publish -c %_C% -o %_P%\dotnet-wix\ -f netcoreapp2.1 src\wix || exit /b %ERRORLEVEL%

dotnet publish -c %_C% -o %_P%\WixToolset.MSBuild\separate\net461\x86\buildtasks\ -f net461 -r win-x86 src\WixToolset.BuildTasks || exit /b %ERRORLEVEL%
dotnet publish -c %_C% -o %_P%\WixToolset.MSBuild\separate\net461\x86\heat\ -f net461 -r win-x86 src\heat || exit /b %ERRORLEVEL%
dotnet publish -c %_C% -o %_P%\WixToolset.MSBuild\separate\net461\x86\wix\ -f net461 -r win-x86 src\wix || exit /b %ERRORLEVEL%
robocopy %_P%\WixToolset.MSBuild\separate\net461\x86\buildtasks %_P%\WixToolset.MSBuild\tools\net461\x86 %_RCO% /XF Microsoft.Build.*.dll
robocopy %_P%\WixToolset.MSBuild\separate\net461\x86\heat %_P%\WixToolset.MSBuild\tools\net461\x86 %_RCO%
robocopy %_P%\WixToolset.MSBuild\separate\net461\x86\wix %_P%\WixToolset.MSBuild\tools\net461\x86 %_RCO%

dotnet publish -c %_C% -o %_P%\WixToolset.MSBuild\separate\net461\x64\buildtasks\ -f net461 -r win-x64 src\WixToolset.BuildTasks || exit /b %ERRORLEVEL%
dotnet publish -c %_C% -o %_P%\WixToolset.MSBuild\separate\net461\x64\heat\ -f net461 -r win-x64 src\heat || exit /b %ERRORLEVEL%
dotnet publish -c %_C% -o %_P%\WixToolset.MSBuild\separate\net461\x64\wix\ -f net461 -r win-x64 src\wix || exit /b %ERRORLEVEL%
robocopy %_P%\WixToolset.MSBuild\separate\net461\x64\buildtasks %_P%\WixToolset.MSBuild\tools\net461\x64 %_RCO% /XF Microsoft.Build.*.dll
robocopy %_P%\WixToolset.MSBuild\separate\net461\x64\heat %_P%\WixToolset.MSBuild\tools\net461\x64 %_RCO%
robocopy %_P%\WixToolset.MSBuild\separate\net461\x64\wix %_P%\WixToolset.MSBuild\tools\net461\x64 %_RCO%

dotnet publish -c %_C% -o %_P%\WixToolset.MSBuild\separate\netcoreapp2.1\buildtasks\ -f netcoreapp2.1 src\WixToolset.BuildTasks || exit /b %ERRORLEVEL%
dotnet publish -c %_C% -o %_P%\WixToolset.MSBuild\separate\netcoreapp2.1\heat\ -f netcoreapp2.1 src\heat || exit /b %ERRORLEVEL%
dotnet publish -c %_C% -o %_P%\WixToolset.MSBuild\separate\netcoreapp2.1\wix\ -f netcoreapp2.1 src\wix || exit /b %ERRORLEVEL%
robocopy %_P%\WixToolset.MSBuild\separate\netcoreapp2.1\buildtasks %_P%\WixToolset.MSBuild\tools\netcoreapp2.1 %_RCO% /XF Microsoft.Build.*.dll
robocopy %_P%\WixToolset.MSBuild\separate\netcoreapp2.1\heat %_P%\WixToolset.MSBuild\tools\netcoreapp2.1 %_RCO%
robocopy %_P%\WixToolset.MSBuild\separate\netcoreapp2.1\wix %_P%\WixToolset.MSBuild\tools\netcoreapp2.1 %_RCO%

dotnet publish -c %_C% -o %_P%\WixToolset.MSBuild\ src\WixToolset.MSBuild || exit /b %ERRORLEVEL%
dotnet publish -c %_C% -o %_P%\WixToolset.MSBuild\broken\net461\ -f net461 -r dne src\wix || exit /b %ERRORLEVEL%

dotnet test -c %_C% src\test\WixToolsetTest.MSBuild || exit /b %ERRORLEVEL%

dotnet pack -c %_C% src\dotnet-wix || exit /b %ERRORLEVEL%
dotnet pack -c %_C% src\WixToolset.MSBuild || exit /b %ERRORLEVEL%

msbuild -p:Configuration=%_C% .\src\ThmViewerPackage\ThmViewerPackage.wixproj || exit /b %ERRORLEVEL%

@popd
@endlocal
