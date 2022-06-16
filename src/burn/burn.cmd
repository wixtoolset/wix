@setlocal
@pushd %~dp0

@set _C=Debug
@set _L=%~dp0..\..\build\logs
:parse_args
@if /i "%1"=="release" set _C=Release
@if not "%1"=="" shift & goto parse_args

@set _B=%~dp0..\..\build\burn\%_C%

@echo Building burn %_C%

:: burn

nuget restore || exit /b

msbuild burn_t.proj -p:Configuration=%_C% -nologo -m -warnaserror -bl:%_L%\burn_build.binlog || exit /b

dotnet test %_B%\x86\BurnUnitTest.dll --nologo -l "trx;LogFileName=%_L%\TestResults\BurnUnitTest32.trx" || exit /b
dotnet test %_B%\x64\BurnUnitTest.dll --nologo -l "trx;LogFileName=%_L%\TestResults\BurnUnitTest64.trx" || exit /b

@popd
@endlocal
