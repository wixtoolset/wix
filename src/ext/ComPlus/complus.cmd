@setlocal
@pushd %~dp0
@set _C=Release
@if /i "%1"=="debug" set _C=Debug

:: Restore
msbuild -p:Configuration=%_C% -t:Restore || exit /b

:: Build
::msbuild -p:Configuration=%_C% -p:Platform=Win32 ca\complusca.vcxproj || exit /b
::msbuild -p:Configuration=%_C% -p:Platform=x64 ca\complusca.vcxproj || exit /b

msbuild -p:Configuration=%_C% test\WixToolsetTest.ComPlus\WixToolsetTest.ComPlus.csproj || exit /b

:: Test
dotnet test -c %_C% --no-build test\WixToolsetTest.ComPlus || exit /b

:: Pack
msbuild -p:Configuration=%_C% -p:NoBuild=true -t:Pack wixext\WixToolset.ComPlus.wixext.csproj || exit /b

@popd
@endlocal