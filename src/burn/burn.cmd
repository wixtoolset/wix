@setlocal
@pushd %~dp0

@set _C=Debug
:parse_args
@if /i "%1"=="release" set _C=Release
@if not "%1"=="" shift & goto parse_args

@echo Building burn %_C%

:: burn

nuget restore || exit /b

msbuild burn_t.proj -p:Configuration=%_C% -nologo -m -warnaserror -bl:..\..\build\logs\burn_build.binlog || exit /b

msbuild test\BurnUnitTest -t:Test -p:Configuration=%_C% -nologo || exit /b

@popd
@endlocal
