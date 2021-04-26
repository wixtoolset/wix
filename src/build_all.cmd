@setlocal
@pushd %~dp0

@set _C=Debug
:parse_args
@if /i "%1"=="release" set _C=Release& shift
@if not "%1"=="" shift & goto parse_args

@echo build %_C%

:: Initialize required files/folders

call build_init.cmd

:: DTF

call dtf\dtf.cmd %_C% || exit /b


:: internal

call internal\internal.cmd %_C% || exit /b


:: libs

call libs\libs.cmd %_C% || exit /b


:: api

call api\api.cmd %_C% || exit /b


:: burn

call burn\burn.cmd %_C% || exit /b


:: wix

call wix\wix.cmd %_C% || exit /b


:: ext

call ext\ext.cmd %_C% || exit /b


:: samples

:: call samples\samples.cmd %_C% || exit /b


:: integration tests

call test\test.cmd %_C% || exit /b

@popd
@endlocal
