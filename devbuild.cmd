@echo off

setlocal
pushd %~dp0

:parse_args
if /i "%1"=="release" set _C=Release
if /i "%1"=="inc" set _INCREMENTAL=1
if /i "%1"=="clean" set _INCREMENTAL= & set _CLEAN=1
if not "%1"=="" shift & goto parse_args

if not "%_INCREMENTAL%"=="1" call src\clean.cmd
if not "%_CLEAN%"=="" goto end

call src\build_all.cmd %_C%

:end
popd
endlocal
