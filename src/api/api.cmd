@setlocal
@pushd %~dp0

@set _C=Debug
:parse_args
@if /i "%1"=="release" set _C=Release
@if not "%1"=="" shift & goto parse_args

@echo Building api %_C%

:: restore
:: build
:: pack
msbuild -m -p:Configuration=%_C% api.proj || exit /b

:: test
dotnet test -c %_C% --no-build burn\test\WixToolsetTest.Mba.Core\WixToolsetTest.Mba.Core.csproj || exit /b
dotnet test -c %_C% --no-build wix\api_wix.sln || exit /b


@popd
@endlocal
