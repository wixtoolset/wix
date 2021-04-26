@setlocal
@pushd %~dp0

@set _C=Debug
:parse_args
@if /i "%1"=="release" set _C=Release& shift
@if not "%1"=="" shift & goto parse_args

@echo Burn integration tests %_C%

msbuild -t:Build -Restore -p:Configuration=%_C% -warnaserror || exit /b
msbuild -t:Build -Restore -p:Configuration=%_C% TestData\TestData.proj || exit /b

if /i "%RuntimeTestsEnabled%"=="true" dotnet test -c %_C% --no-build src\WixToolsetTest.BurnE2E

@popd
@endlocal
