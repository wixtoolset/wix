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

msbuild api_t.proj -p:Configuration=%_C% -nologo -m -bl:..\..\build\logs\api_build.binlog || exit /b

:: test
dotnet test burn\test\WixToolsetTest.Mba.Core\WixToolsetTest.Mba.Core.csproj -c %_C% --nologo --no-build || exit /b
dotnet test wix\api_wix.sln -c %_C% --nologo --no-build || exit /b

@popd
@endlocal
