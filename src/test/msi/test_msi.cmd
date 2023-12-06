@setlocal
@pushd %~dp0

@set _RESULT=0
@set _C=Debug
@set _L=%~dp0..\..\..\build\logs
:parse_args
@if /i "%1"=="release" set _C=Release
@if /i "%1"=="test" set RuntimeTestsEnabled=true
@if not "%1"=="" shift & goto parse_args

@echo Msi integration tests %_C%

msbuild -t:Build -Restore -p:Configuration=%_C% -tl -nologo -warnaserror -bl:%_L%\test_msi_build.binlog || exit /b
msbuild -t:Build -Restore TestData\TestData.proj -p:Configuration=%_C% -tl -nologo -m -warnaserror -bl:%_L%\test_msi_data_build.binlog || exit /b

@if not "%RuntimeTestsEnabled%"=="true" goto :LExit

dotnet test -c %_C% WixToolsetTest.MsiE2E --nologo --no-build -l "trx;LogFileName=%_L%\TestResults\WixToolsetTest.MsiE2E.trx" || exit /b

:LExit
@popd
@endlocal
