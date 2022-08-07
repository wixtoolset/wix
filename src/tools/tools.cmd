@setlocal
@pushd %~dp0

@set _C=Debug
@set _L=%~dp0..\..\build\logs

:parse_args
@if /i "%1"=="release" set _C=Release
@if /i "%1"=="inc" set _INCREMENTAL=1
@if /i "%1"=="clean" set _INCREMENTAL= & set _CLEAN=1
@if not "%1"=="" shift & goto parse_args

:: Clean
@if NOT "%_INCREMENTAL%"=="" call :clean
@if NOT "%_CLEAN%"=="" goto :end

@echo Building tools %_C%

:: Build
msbuild -Restore tools.sln -p:Configuration=%_C% -nologo -m -warnaserror -bl:%_L%\tools_build.binlog || exit /b

:: Publish
msbuild publish_t.proj -p:Configuration=%_C% -nologo -m -warnaserror -bl:%_L%\tools_publish.binlog || exit /b

:: Test
dotnet test -c %_C% --no-build --nologo test\WixToolsetTest.HeatTasks -l "trx;LogFileName=%_L%\TestResults\WixToolsetTest.HeatTasks.trx" || exit /b

:: Pack
msbuild -t:Pack WixToolset.Heat -p:Configuration=%_C% -p:NoBuild=true -nologo -m -warnaserror -bl:%_L%\tools_pack.binlog || exit /b

@goto :end

:clean
@rd /s/q "..\..\build\tools" 2> nul
@del "..\..\build\artifacts\WixToolset.Heat.*.nupkg" 2> nul
@rd /s/q "%USERPROFILE%\.nuget\packages\wixtoolset.heat" 2> nul
@exit /b

:end
@popd
@endlocal
