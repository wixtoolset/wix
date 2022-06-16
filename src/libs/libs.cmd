@setlocal
@pushd %~dp0

@set _C=Debug
@set _L=%~dp0..\..\build\logs
:parse_args
@if /i "%1"=="release" set _C=Release
@if not "%1"=="" shift & goto parse_args

@set _B=%~dp0..\..\build\libs\%_C%

@echo Building libs %_C%

msbuild -Restore libs_t.proj -p:Configuration=%_C% -nologo -m -warnaserror -bl:%_L%\libs_build.binlog || exit /b

dotnet test %_B%\x86\DUtilUnitTest.dll --nologo -l "trx;LogFileName=%_L%\TestResults\DutilUnitTest32.trx" || exit /b
dotnet test %_B%\x64\DUtilUnitTest.dll --nologo -l "trx;LogFileName=%_L%\TestResults\DutilUnitTest64.trx" || exit /b

@popd
@endlocal
