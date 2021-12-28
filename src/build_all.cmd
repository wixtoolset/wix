@setlocal
@pushd %~dp0

@set _C=Debug
:parse_args
@if /i "%1"=="release" set _C=Release
@if not "%1"=="" shift & goto parse_args

@if "%VCToolsVersion%"=="" call :StartDeveloperCommandPrompt || exit /b

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

call samples\samples.cmd %_C% || exit /b


:: integration tests

call test\test.cmd %_C% || exit /b

goto LExit

:StartDeveloperCommandPrompt
if not "%WixSkipVsDevCmd%"=="" (
  echo Skipping initializing developer command prompt
  exit /b
)

echo Initializing developer command prompt

if not exist "%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" (
  "%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
  exit /b 2
)

for /f "usebackq delims=" %%i in (`"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" -version [17.0^,18.0^) -property installationPath`) do (
  if exist "%%i\Common7\Tools\vsdevcmd.bat" (
    call "%%i\Common7\Tools\vsdevcmd.bat" -no_logo
    exit /b
  )
  echo developer command prompt not found in %%i
)

echo No versions of developer command prompt found
exit /b 2

:LExit
@popd
@endlocal
