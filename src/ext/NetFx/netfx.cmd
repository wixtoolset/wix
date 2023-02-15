@setlocal
@pushd %~dp0

@set _C=Debug
@set _L=%~dp0..\..\..\build\logs

:parse_args
@if /i "%1"=="release" set _C=Release
@if /i "%1"=="inc" set _INC=1
@if /i "%1"=="clean" set _CLEAN=1
@if not "%1"=="" shift & goto parse_args

@set _B=%~dp0..\..\..\build\NetFx.wixext\%_C%

:: Clean

@if "%_INC%"=="" call :clean
@if NOT "%_CLEAN%"=="" goto :end

@echo NetFx.wixext build %_C%

:: Restore
nuget restore netcoresearch\packages.config || exit /b

:: Build
msbuild -Restore -p:Configuration=%_C% -bl:%_L%\ext_netfx_build.binlog || exit /b

:: Test
dotnet test ^
 %_B%\net6.0\WixToolsetTest.NetFx.dll ^
 --nologo -l "trx;LogFileName=%_L%\TestResults\netfx.wixext.trx" || exit /b

:: Pack
msbuild -t:Pack -p:Configuration=%_C% -p:NoBuild=true wixext\WixToolset.Netfx.wixext.csproj || exit /b

@goto :end

:clean
@rd /s/q "..\..\build\NetFx.wixext" 2> nul
@del "..\..\build\artifacts\WixToolset.NetFx.wixext.*.nupkg" 2> nul
@del "%_L%\ext_netfx_build.binlog" 2> nul
@del "%_L%\TestResults\netfx.wixext.trx" 2> nul
@rd /s/q "%USERPROFILE%\.nuget\packages\wixtoolset.netfx.wixext" 2> nul
@exit /b

:end
@popd
@endlocal
