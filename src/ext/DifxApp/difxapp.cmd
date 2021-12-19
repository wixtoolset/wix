@setlocal
@pushd %~dp0

@set _C=Debug
:parse_args
@if /i "%1"=="release" set _C=Release
@if not "%1"=="" shift & goto parse_args

@echo DifxApp.wixext build %_C%

:: Build
msbuild -Restore -p:Configuration=%_C% || exit /b

:: Test
dotnet test -c %_C% --no-build test\WixToolsetTest.DifxApp || exit /b

:: Pack
msbuild -t:Pack -p:Configuration=%_C% -p:NoBuild=true wixext\WixToolset.DifxApp.wixext.csproj || exit /b

@popd
@endlocal
