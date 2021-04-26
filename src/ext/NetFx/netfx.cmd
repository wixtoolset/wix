@setlocal
@pushd %~dp0

@set _C=Debug
:parse_args
@if /i "%1"=="release" set _C=Release& shift
@if not "%1"=="" shift & goto parse_args

@echo Building ext\NetFx %_C%

:: Restore
msbuild -t:Restore -p:Configuration=%_C% || exit /b

:: Build
msbuild -t:Build -p:Configuration=%_C% test\WixToolsetTest.Netfx\WixToolsetTest.Netfx.csproj || exit /b

:: Test
dotnet test -c %_C% --no-build test\WixToolsetTest.Netfx || exit /b

:: Pack
msbuild -t:Pack -p:Configuration=%_C% -p:NoBuild=true wixext\WixToolset.Netfx.wixext.csproj || exit /b

@popd
@endlocal
