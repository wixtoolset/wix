@setlocal
@pushd %~dp0

@set _C=Debug
:parse_args
@if /i "%1"=="release" set _C=Release
@if /i "%1"=="test" set RuntimeTestsEnabled=true
@if not "%1"=="" shift & goto parse_args

@if not "%RuntimeTestsEnabled%"=="true" echo Build integration tests %_C%
@if "%RuntimeTestsEnabled%"=="true" set _T=test&echo Run integration tests %_C%

@call burn\test_burn.cmd %_C% %_T% || exit /b

dotnet test wix -c %_C% --nologo || exit /b

@popd
@endlocal
