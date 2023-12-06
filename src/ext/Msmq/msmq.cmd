@setlocal
@pushd %~dp0

@set _C=Debug
:parse_args
@if /i "%1"=="release" set _C=Release
@if not "%1"=="" shift & goto parse_args

@echo Msmq.wixext build %_C%

:: Build
msbuild -Restore -p:Configuration=%_C% -tl -nologo -warnaserror || exit /b

:: Test
dotnet test test\WixToolsetTest.Msmq -c %_C% --no-build --nologo || exit /b

:: Pack
dotnet pack wixext\WixToolset.Msmq.wixext.csproj -c %_C% --no-build --nologo || exit /b

@popd
@endlocal
