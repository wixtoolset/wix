@setlocal
@pushd %~dp0

@set _C=Debug
@set _L=%~dp0..\..\build\logs
:parse_args
@if /i "%1"=="release" set _C=Release
@if /i "%1"=="test" set RuntimeTestsEnabled=true
@if not "%1"=="" shift & goto parse_args

@if not "%RuntimeTestsEnabled%"=="true" echo Build integration tests %_C%
@if "%RuntimeTestsEnabled%"=="true" set _T=test&echo Run integration tests %_C%

@call msi\test_msi.cmd %_C% %_T% || exit /b
@call burn\test_burn.cmd %_C% %_T% || exit /b

dotnet test wix -c %_C% --nologo -l "trx;LogFileName=%_L%\TestResults\WixToolsetTest.WixE2ETests.trx" || exit /b

@popd
@endlocal
