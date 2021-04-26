@setlocal
@pushd %~dp0

@set _C=Debug
:parse_args
@if /i "%1"=="release" set _C=Release& shift
@if not "%1"=="" shift & goto parse_args

@echo Building ext\Util %_C% using %_N%

:: Restore
msbuild -t:Restore -p:Configuration=%_C% || exit /b

:: Build
msbuild -t:Build -p:Configuration=%_C% test\WixToolsetTest.Util\WixToolsetTest.Util.csproj || exit /b

:: Test
dotnet test -c %_C% --no-build test\WixToolsetTest.Util || exit /b

:: Pack
msbuild -p:Configuration=%_C% -p:NoBuild=true -t:Pack wixext\WixToolset.Util.wixext.csproj || exit /b

@popd
@endlocal
