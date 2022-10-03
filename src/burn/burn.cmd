@setlocal
@pushd %~dp0

@set _C=Debug
@set _L=%~dp0..\..\build\logs

:parse_args
@if /i "%1"=="release" set _C=Release
@if /i "%1"=="inc" set _INC=1
@if /i "%1"=="clean" set _CLEAN=1
@if not "%1"=="" shift & goto parse_args

@set _B=%~dp0..\..\build\burn\%_C%

:: Clean

@if "%_INC%"=="" call :clean
@if NOT "%_CLEAN%"=="" goto :end

@echo Building burn %_C%

:: burn

nuget restore || exit /b

msbuild burn_t.proj -p:Configuration=%_C% -nologo -m -warnaserror -bl:%_L%\burn_build.binlog || exit /b

dotnet test ^
 %_B%\x86\BurnUnitTest.dll ^
 %_B%\x64\BurnUnitTest.dll ^
 --nologo -l "trx;LogFileName=%_L%\TestResults\burn.trx" || exit /b

@goto :end

:clean
@rd /s/q "..\..\build\burn" 2> nul
@del "..\..\build\artifacts\WixToolset.Burn.*.nupkg" 2> nul
@del "%_L%\TestResults\burn.trx" 2> nul
@rd /s/q "%USERPROFILE%\.nuget\packages\wixtoolset.burn" 2> nul
@exit /b

:end
@popd
@endlocal
