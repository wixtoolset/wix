@setlocal
@pushd %~dp0
@set _C=Release
@if /i "%1"=="debug" set _C=Debug

:: Restore
msbuild -p:Configuration=%_C% -t:Restore || exit /b

:: Build
msbuild -p:Configuration=%_C% src\test\WixToolsetTest.Core.Native\WixToolsetTest.Core.Native.csproj || exit /b

:: Test
dotnet test -c %_C% --no-build src\test\WixToolsetTest.Core.Native\WixToolsetTest.Core.Native.csproj || exit /b

:: Pack
msbuild -p:Configuration=%_C% -p:NoBuild=true -t:Pack src\WixToolset.Core.Native\WixToolset.Core.Native.csproj || exit /b

@popd
@endlocal
