@setlocal
@pushd %~dp0

@set _RESULT=0
@set _C=Debug
@set _L=%~dp0..\..\..\build\logs
:parse_args
@if /i "%1"=="release" set _C=Release
@if /i "%1"=="test" set RuntimeTestsEnabled=true
@if not "%1"=="" shift & goto parse_args

@set _B=%~dp0..\..\..\build\IntegrationBurn\%_C%

@echo Burn integration tests %_C%

msbuild -Restore -p:Configuration=%_C% -tl -nologo -m -warnaserror -bl:%_L%\test_burn_build.binlog || exit /b
msbuild -Restore TestData\TestData.proj -p:Configuration=%_C% -tl -nologo -m -warnaserror -bl:%_L%\test_burn_data_build.binlog || exit /b

"%_B%\net462\win-x86\testexe.exe" /dm "%_B%\net6.0-windows\testhost.exe"
mt.exe -manifest "WixToolsetTest.BurnE2E\testhost.longpathaware.manifest" -updateresource:"%_B%\net6.0-windows\testhost.exe"

@if not "%RuntimeTestsEnabled%"=="true" goto :LExit

dotnet test -c %_C% WixToolsetTest.BurnE2E --nologo --no-build -l "trx;LogFileName=%_L%\TestResults\WixToolsetTest.BurnE2E.trx" || exit /b

:LExit
@popd
@endlocal
