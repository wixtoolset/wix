@setlocal
@pushd %~dp0
@set _C=Release
@if /i "%1"=="debug" set _C=Debug
@set _P=%~dp0build\%_C%\publish
@set _RCO=/S /R:1 /W:1 /NP /XO  /NS /NC /NFL /NDL /NJH /NJS

:: Restore
nuget restore || exit /b

:: Build
msbuild -p:Configuration=%_C% || exit /b

:: Test
dotnet test -c %_C% --no-build src\test\WixToolsetTest.BuildTasks || exit /b

dotnet publish -c %_C% -o %_P%\dotnet-wix\ -f netcoreapp3.1 src\wix || exit /b

dotnet publish -c %_C% -o %_P%\WixToolset.Sdk\separate\net461\x86\buildtasks\ -f net461 -r win-x86 src\WixToolset.BuildTasks || exit /b
dotnet publish -c %_C% -o %_P%\WixToolset.Sdk\separate\net461\x86\heat\ -f net461 -r win-x86 src\heat || exit /b
dotnet publish -c %_C% -o %_P%\WixToolset.Sdk\separate\net461\x86\wix\ -f net461 -r win-x86 src\wix || exit /b
robocopy %_P%\WixToolset.Sdk\separate\net461\x86\buildtasks %_P%\WixToolset.Sdk\tools\net461\x86 %_RCO% /XF Microsoft.Build.*.dll
robocopy %_P%\WixToolset.Sdk\separate\net461\x86\heat %_P%\WixToolset.Sdk\tools\net461\x86 %_RCO%
robocopy %_P%\WixToolset.Sdk\separate\net461\x86\wix %_P%\WixToolset.Sdk\tools\net461\x86 %_RCO%

dotnet publish -c %_C% -o %_P%\WixToolset.Sdk\separate\net461\x64\buildtasks\ -f net461 -r win-x64 src\WixToolset.BuildTasks || exit /b
dotnet publish -c %_C% -o %_P%\WixToolset.Sdk\separate\net461\x64\heat\ -f net461 -r win-x64 src\heat || exit /b
dotnet publish -c %_C% -o %_P%\WixToolset.Sdk\separate\net461\x64\wix\ -f net461 -r win-x64 src\wix || exit /b
robocopy %_P%\WixToolset.Sdk\separate\net461\x64\buildtasks %_P%\WixToolset.Sdk\tools\net461\x64 %_RCO% /XF Microsoft.Build.*.dll
robocopy %_P%\WixToolset.Sdk\separate\net461\x64\heat %_P%\WixToolset.Sdk\tools\net461\x64 %_RCO%
robocopy %_P%\WixToolset.Sdk\separate\net461\x64\wix %_P%\WixToolset.Sdk\tools\net461\x64 %_RCO%

dotnet publish -c %_C% -p:UseAppHost=false -o %_P%\WixToolset.Sdk\separate\netcoreapp3.1\buildtasks\ -f netcoreapp3.1 src\WixToolset.BuildTasks || exit /b
dotnet publish -c %_C% -p:UseAppHost=false -o %_P%\WixToolset.Sdk\separate\netcoreapp3.1\heat\ -f netcoreapp3.1 src\heat || exit /b
dotnet publish -c %_C% -p:UseAppHost=false -o %_P%\WixToolset.Sdk\separate\netcoreapp3.1\wix\ -f netcoreapp3.1 src\wix || exit /b
robocopy %_P%\WixToolset.Sdk\separate\netcoreapp3.1\buildtasks %_P%\WixToolset.Sdk\tools\netcoreapp3.1 %_RCO% /XF Microsoft.Build.*.dll
robocopy %_P%\WixToolset.Sdk\separate\netcoreapp3.1\heat %_P%\WixToolset.Sdk\tools\netcoreapp3.1 %_RCO%
robocopy %_P%\WixToolset.Sdk\separate\netcoreapp3.1\wix %_P%\WixToolset.Sdk\tools\netcoreapp3.1 %_RCO%

dotnet publish -c %_C% -o %_P%\WixToolset.Sdk\ src\WixToolset.Sdk || exit /b
dotnet publish -c %_C% -o %_P%\WixToolset.Sdk\broken\net461\ -f net461 -r linux-x64 src\wix || exit /b

dotnet test -c %_C% src\test\WixToolsetTest.Sdk || exit /b

:: Pack
dotnet pack -c %_C% src\dotnet-wix || exit /b
dotnet pack -c %_C% src\WixToolset.Sdk || exit /b

msbuild -p:Configuration=%_C% .\src\ThmViewerPackage\ThmViewerPackage.wixproj || exit /b

@popd
@endlocal
