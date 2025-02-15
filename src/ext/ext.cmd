@setlocal
@pushd %~dp0

@set _C=Debug
@set _L=%~dp0..\..\build\logs
@set _SuppressWixClean=false

:parse_args
@if /i "%1"=="release" set _C=Release
@if /i "%1"=="inc" set _SuppressWixClean=true
@if not "%1"=="" shift & goto parse_args

msbuild ext_t.proj -p:Configuration=%_C% -p:SuppressWixClean=%_SuppressWixClean% -m -tl -nologo -warnaserror -bl:%_L%\ext_build.binlog || exit /b

@popd
@endlocal
