@setlocal
@pushd %~dp0

@set _C=Debug
:parse_args
@if /i "%1"=="release" set _C=Release
@if not "%1"=="" shift & goto parse_args

@echo Building libs %_C% using %_N%

:: libs

nuget restore || exit /b

msbuild -m -p:Configuration=%_C% libs.proj || exit /b

@popd
@endlocal
