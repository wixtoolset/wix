@echo off

setlocal
pushd %~dp0

if /i "%1"=="release" set _C=Release

src\build_all.cmd %_C%

popd
endlocal
