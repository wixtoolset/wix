@setlocal
@pushd %~dp0

@set _C=Debug
:parse_args
@if /i "%1"=="release" set _C=Release
@if not "%1"=="" shift & goto parse_args

@echo Building ext\Util %_C% using %_N%

:: Build
msbuild -Restore -p:Configuration=%_C% || exit /b

:: Test
dotnet test -c %_C% --no-build test\WixToolsetTest.Util || exit /b

:: Pack
msbuild -t:Pack -p:Configuration=%_C% -p:NoBuild=true wixext\WixToolset.Util.wixext.csproj || exit /b

@popd
@endlocal
