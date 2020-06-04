@setlocal
@pushd %~dp0
@set _P=%~dp0build\Release\publish

dotnet test -c Release src\test\WixToolsetTest.CoreIntegration || exit /b %ERRORLEVEL%

dotnet pack -c Release src\WixToolset.Core || exit /b %ERRORLEVEL%
dotnet pack -c Release src\WixToolset.Core.Burn || exit /b %ERRORLEVEL%
dotnet pack -c Release src\WixToolset.Core.WindowsInstaller || exit /b %ERRORLEVEL%

dotnet pack -c Release src\WixToolset.Core.TestPackage || exit /b %ERRORLEVEL%

@popd
@endlocal
