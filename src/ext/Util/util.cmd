@setlocal
@pushd %~dp0

@set _C=Debug
@set _L=%~dp0..\..\..\build\logs

:parse_args
@if /i "%1"=="release" set _C=Release
@if /i "%1"=="inc" set _INC=1
@if /i "%1"=="clean" set _CLEAN=1
@if not "%1"=="" shift & goto parse_args

@set _B=%~dp0..\..\..\build\Util.wixext\%_C%

:: Clean

@if "%_INC%"=="" call :clean
@if NOT "%_CLEAN%"=="" goto :end

@echo Building ext\Util %_C% using %_N%

:: Build
msbuild -Restore -p:Configuration=%_C% -warnaserror -bl:%_L%\ext_util_build.binlog || exit /b

:: Test
dotnet test ^
 %_B%\net6.0\WixToolsetTest.Util.dll ^
 --nologo -l "trx;LogFileName=%_L%\TestResults\util.wixext.trx" || exit /b

:: Pack
msbuild -t:Pack -p:Configuration=%_C% -p:NoBuild=true wixext\WixToolset.Util.wixext.csproj || exit /b

@goto :end

:clean
@rd /s/q "..\..\build\Util.wixext" 2> nul
@del "..\..\build\artifacts\WixToolset.Util.wixext.*.nupkg" 2> nul
@del "%_L%\ext_util_build.binlog" 2> nul
@del "%_L%\TestResults\util.wixext.trx" 2> nul
@rd /s/q "%USERPROFILE%\.nuget\packages\wixtoolset.util.wixext" 2> nul
@exit /b

:end
@popd
@endlocal
