@setlocal
@pushd %~dp0

@set _C=Debug
:parse_args
@if /i "%1"=="release" set _C=Release
@if not "%1"=="" shift & goto parse_args

@echo Building libs %_C%

msbuild -Restore libs_t.proj -p:Configuration=%_C% -nologo -m -bl:..\..\build\logs\libs_build.binlog || exit /b

@popd
@endlocal
