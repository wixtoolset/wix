@setlocal
@pushd %~dp0

@set _C=Debug
@set _L=%~dp0..\..\build\logs

:parse_args
@if /i "%1"=="release" set _C=Release
@if /i "%1"=="inc" set _INCREMENTAL=1
@if /i "%1"=="clean" set _INCREMENTAL= & set _CLEAN=1
@if not "%1"=="" shift & goto parse_args

:: Clean
@if NOT "%_INCREMENTAL%"=="" call :clean
@if NOT "%_CLEAN%"=="" goto :end

@echo Building setup %_C%

:: Build
msbuild -Restore setup.sln -p:Configuration=%_C% -nologo -m -warnaserror -bl:%_L%\setup_build.binlog || exit /b

:: Publish


:: Test


:: Pack


@goto :end

:clean
@rd /s/q "..\..\build\setup" 2> nul
@del "..\..\build\artifacts\WixAdditionalTools.*" 2> nul
@del "..\..\build\logs\pdbs\%_C%\WixAdditionalTools.*" 2> nul
@exit /b

:end
@popd
@endlocal
