@setlocal
@pushd %~dp0

@set _C=Debug
:parse_args
@if /i "%1"=="release" set _C=Release& shift
@if not "%1"=="" shift & goto parse_args

@echo Building api %_C%

:: burn

nuget restore burn\api_burn.sln || exit /b

msbuild -p:Configuration=%_C%;Platform=x86;PlatformToolset=v142 burn\api_burn.sln || exit /b
msbuild -p:Configuration=%_C%;Platform=x64;PlatformToolset=v142 burn\api_burn.sln || exit /b
msbuild -p:Configuration=%_C%;Platform=ARM64;PlatformToolset=v142 burn\api_burn.sln || exit /b

msbuild -p:Configuration=%_C%;Platform=x86;PlatformToolset=v141 burn\api_burn.sln || exit /b
msbuild -p:Configuration=%_C%;Platform=x64;PlatformToolset=v141 burn\api_burn.sln || exit /b
msbuild -p:Configuration=%_C%;Platform=ARM64;PlatformToolset=v141 burn\api_burn.sln || exit /b

msbuild -p:Configuration=%_C%;Platform=x86;PlatformToolset=v140 burn\api_burn.sln || exit /b
msbuild -p:Configuration=%_C%;Platform=x64;PlatformToolset=v140 burn\api_burn.sln || exit /b

dotnet test -c %_C% --no-build burn\test\WixToolsetTest.Mba.Core\WixToolsetTest.Mba.Core.csproj || exit /b

msbuild -t:PackNative -p:Configuration=%_C% burn\balutil\balutil.vcxproj || exit /b
msbuild -t:PackNative -p:Configuration=%_C% burn\bextutil\bextutil.vcxproj || exit /b
msbuild -t:PackNative -Restore -p:Configuration=%_C% burn\WixToolset.BootstrapperCore.Native\WixToolset.BootstrapperCore.Native.proj || exit /b
msbuild -t:Pack -p:Configuration=%_C% -p:NoBuild=true burn\WixToolset.Mba.Core\WixToolset.Mba.Core.csproj || exit /b


:: wix

msbuild -Restore -p:Configuration=%_C% wix\api_wix.sln || exit /b

dotnet test -c %_C% --no-build wix\api_wix.sln || exit /b

msbuild -t:Pack -p:Configuration=%_C% -p:NoBuild=true wix\api_wix.sln || exit /b

@popd
@endlocal
