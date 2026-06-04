@setlocal
@pushd %~dp0

@set _RESULT=0
@set _C=Debug
@set _L=%~dp0..\..\..\build\logs
:parse_args
@if /i "%1"=="release" set _C=Release
@if not "%1"=="" shift & goto parse_args

@echo WiX integration tests %_C%

msbuild -Restore WixE2E\WixE2E.csproj -t:Test -p:Configuration=%_C% -tl -nologo -warnaserror -bl:%_L%\test_wix.binlog || exit /b

@popd
@endlocal
