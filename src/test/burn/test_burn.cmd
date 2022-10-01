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

msbuild -t:Build -Restore -p:Configuration=%_C% -warnaserror -bl:%_L%\test_burn_build.binlog || exit /b
msbuild -t:Build -Restore TestData\TestData.proj -p:Configuration=%_C% -m -bl:%_L%\test_burn_data_build.binlog || exit /b

"%_B%\net35\win-x86\testexe.exe" /dm "%_B%\net6.0-windows\testhost.exe"
mt.exe -manifest "WixToolsetTest.BurnE2E\testhost.longpathaware.manifest" -updateresource:"%_B%\net6.0-windows\testhost.exe"

@if not "%RuntimeTestsEnabled%"=="true" goto :LExit

dotnet test -c %_C% --no-build WixToolsetTest.BurnE2E -l "trx;LogFileName=%_L%\TestResults\WixToolsetTest.BurnE2E.trx" || exit /b

:LExit
@popd
@endlocal
