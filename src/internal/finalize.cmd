@setlocal
@pushd %~dp0

@set _C=Debug
:parse_args
@if /i "%1"=="release" set _C=Release
@if not "%1"=="" shift & goto parse_args

@echo Finalize build %_C%

msbuild -Restore WixBuildFinalize\WixBuildFinalize.proj -p:Configuration=%_C% -nologo || exit /b

@popd
@endlocal
