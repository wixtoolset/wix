@setlocal
@pushd %~dp0

@set _C=Debug
@set _L=%~dp0..\..\build\logs
:parse_args
@if /i "%1"=="release" set _C=Release
@if not "%1"=="" shift & goto parse_args

@echo Building api %_C%

:: restore
:: build
:: pack

msbuild api_t.proj -p:Configuration=%_C% -nologo -m -warnaserror -bl:%_L%\api_build.binlog || exit /b

:: test
dotnet test burn\test\WixToolsetTest.Mba.Core -c %_C% --nologo --no-build -l "trx;LogFileName=%_L%\TestResults\WixToolsetTest.Mba.Core.trx" || exit /b
msbuild burn\test\BalUtilUnitTest -t:Test -p:Configuration=%_C% -nologo -p:CppCliTestResultsFile="%_L%\TestResults\BalUtilUnitTest.xunit2.xml" || exit /b
msbuild burn\test\BextUtilUnitTest -t:Test -p:Configuration=%_C% -nologo -p:CppCliTestResultsFile="%_L%\TestResults\BextUtilUnitTest.xunit2.xml" || exit /b
dotnet test wix\api_wix.sln -c %_C% --nologo --no-build -l "trx;LogFileName=%_L%\TestResults\api_wix.trx" || exit /b

@popd
@endlocal
