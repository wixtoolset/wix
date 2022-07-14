@setlocal
@pushd %~dp0

@set _C=Debug
:parse_args
@if /i "%1"=="release" set _C=Release
@if not "%1"=="" shift & goto parse_args

@echo Building tools %_C%

:: tools

nuget restore || exit /b

msbuild -t:Build -p:Configuration=%_C% -nologo -m -warnaserror -bl:..\..\build\logs\tools_build.binlog || exit /b

@popd
@endlocal
