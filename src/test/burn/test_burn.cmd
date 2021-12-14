@setlocal
@pushd %~dp0

@set _C=Debug
:parse_args
@if /i "%1"=="release" set _C=Release
@if /i "%1"=="test" set RuntimeTestsEnabled=true
@if not "%1"=="" shift & goto parse_args

@echo Burn integration tests %_C%

msbuild -t:Build -Restore -p:Configuration=%_C% -warnaserror || exit /b
msbuild -t:Build -Restore -p:Configuration=%_C% TestData\TestData.proj || exit /b

@if not "%RuntimeTestsEnabled%"=="true" goto :LExit

reg add HKLM\Software\Policies\Microsoft\Windows\Installer /t REG_SZ /v Logging /d voicewarmupx /f
reg add HKLM\Software\WOW6432Node\Policies\Microsoft\Windows\Installer /t REG_SZ /v Logging /d voicewarmupx /f

dotnet test -c %_C% --no-build WixToolsetTest.BurnE2E

7z a "..\..\..\build\logs\test_burn_%GITHUB_RUN_ID%.zip" "%TEMP%\*.log" "%TEMP%\..\*.log"

:LExit
@popd
@endlocal
