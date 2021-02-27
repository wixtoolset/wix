@setlocal
@pushd %~dp0
@set _C=Release
@if /i "%1"=="debug" set _C=Debug

:: Restore
msbuild -p:Configuration=%_C% -t:Restore || exit /b

:: Build
msbuild -p:Configuration=%_C% src\test\WixToolsetTest.Dependency\WixToolsetTest.Dependency.csproj || exit /b

:: Test
dotnet test -c %_C% --no-build src\test\WixToolsetTest.Dependency || exit /b

:: Pack
msbuild -p:Configuration=%_C% -p:NoBuild=true -t:Pack src\wixext\WixToolset.Dependency.wixext.csproj || exit /b

@popd
@endlocal
