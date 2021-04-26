@setlocal
@pushd %~dp0

@set _C=Debug
:parse_args
@if /i "%1"=="release" set _C=Release& shift
@if not "%1"=="" shift & goto parse_args

@echo Building ext\UI %_C%

:: Restore
@REM nuget restore || exit /b
msbuild -t:Restore -p:RestorePackagesConfig=true -p:Configuration=%_C% || exit /b

:: Build
msbuild -t:Build -p:Configuration=%_C% test\WixToolsetTest.UI\WixToolsetTest.UI.csproj || exit /b

:: Test
dotnet test -c %_C% --no-build test\WixToolsetTest.UI || exit /b

:: Pack
msbuild -t:Pack -p:Configuration=%_C% wixext\WixToolset.UI.wixext.csproj || exit /b

@popd
@endlocal
