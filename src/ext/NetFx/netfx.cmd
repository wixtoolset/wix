@setlocal
@pushd %~dp0

@set _C=Debug
@set _L=%~dp0..\..\..\build\logs
:parse_args
@if /i "%1"=="release" set _C=Release
@if not "%1"=="" shift & goto parse_args

@echo NetFx.wixext build %_C%

:: Restore
nuget restore netcoresearch\packages.config || exit /b
msbuild -t:Restore -p:Configuration=%_C% || exit /b

:: Build
msbuild -p:Configuration=%_C% -bl:%_L%\netfx_build.binlog || exit /b

:: Test
dotnet test -c %_C% --no-build test\WixToolsetTest.Netfx || exit /b

:: Pack
msbuild -t:Pack -p:Configuration=%_C% -p:NoBuild=true wixext\WixToolset.Netfx.wixext.csproj || exit /b

@popd
@endlocal
