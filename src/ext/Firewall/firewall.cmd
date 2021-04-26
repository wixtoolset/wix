@setlocal
@pushd %~dp0

@set _C=Debug
:parse_args
@if /i "%1"=="release" set _C=Release& shift
@if not "%1"=="" shift & goto parse_args

@echo Firewall.wixext build %_C%

:: Restore
msbuild -t:Restore -p:Configuration=%_C% || exit /b

:: Build
msbuild -t:Build -p:Configuration=%_C% test\WixToolsetTest.Firewall\WixToolsetTest.Firewall.csproj || exit /b

:: Test
dotnet test -c %_C% --no-build test\WixToolsetTest.Firewall || exit /b

:: Pack
msbuild -t:Pack -p:Configuration=%_C% wixext\WixToolset.Firewall.wixext.csproj || exit /b

@popd
@endlocal
