@setlocal
@pushd %~dp0

@set _C=Debug
@set _L=%~dp0..\..\build\logs

:parse_args
@if /i "%1"=="release" set _C=Release
@if /i "%1"=="inc" set _INC=1
@if /i "%1"=="clean" set _CLEAN=1
@if not "%1"=="" shift & goto parse_args

@set _B=%~dp0..\..\build\tools\%_C%

:: Clean

@if "%_INC%"=="" call :clean
@if NOT "%_CLEAN%"=="" goto :end

@echo Building tools %_C%

:: Build, Publish, Test, Pack
msbuild -Restore tools_t.proj -p:Configuration=%_C% -tl -nologo -m -warnaserror -bl:%_L%\tools.binlog || exit /b

@goto :end

:clean
@rd /s/q "..\..\build\tools" 2> nul
@exit /b

:end
@popd
@endlocal
