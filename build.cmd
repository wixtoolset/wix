@echo off

setlocal
pushd %~dp0

:parse_args
if /i "%1"=="release" set _C=Release
if /i "%1"=="clean" set _CLEAN=1
if not "%1"=="" shift & goto parse_args

if not "%_CLEAN%"=="" src\clean.cmd
src\build_all.cmd %_C%

popd
endlocal
