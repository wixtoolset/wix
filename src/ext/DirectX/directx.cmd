@setlocal
@pushd %~dp0

@set _C=Debug
:parse_args
@if /i "%1"=="release" set _C=Release
@if not "%1"=="" shift & goto parse_args

@echo DirectX.wixext build %_C%

:: Restore
msbuild -t:Restore -p:Configuration=%_C% -tl -nologo -warnaserror || exit /b

:: Build
msbuild -t:Build -p:Configuration=%_C% -tl -nologo -warnaserror test\WixToolsetTest.DirectX\WixToolsetTest.DirectX.csproj || exit /b

:: Test
dotnet test -c %_C% --no-build test\WixToolsetTest.DirectX || exit /b

:: Pack
msbuild -t:Pack -p:Configuration=%_C% -tl -nologo -warnaserror wixext\WixToolset.DirectX.wixext.csproj || exit /b

@popd
@endlocal
