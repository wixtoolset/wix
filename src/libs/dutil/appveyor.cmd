@setlocal
@pushd %~dp0
@set _C=Release
@if /i "%1"=="debug" set _C=Debug

nuget restore || exit /b

msbuild -t:Test -p:Configuration=%_C% src\test\DUtilUnitTest || exit /b

msbuild -p:Configuration=%_C%;Platform=x86;PlatformToolset=v140 || exit /b
msbuild -p:Configuration=%_C%;Platform=x64;PlatformToolset=v140 || exit /b

msbuild -p:Configuration=%_C%;Platform=x86;PlatformToolset=v141 || exit /b
msbuild -p:Configuration=%_C%;Platform=x64;PlatformToolset=v141 || exit /b
msbuild -p:Configuration=%_C%;Platform=ARM64;PlatformToolset=v141 || exit /b

msbuild -p:Configuration=%_C%;Platform=x86;PlatformToolset=v142 || exit /b
msbuild -p:Configuration=%_C%;Platform=x64;PlatformToolset=v142 || exit /b
msbuild -p:Configuration=%_C%;Platform=ARM64;PlatformToolset=v142 || exit /b

msbuild -p:Configuration=%_C% -t:PackNative src\dutil\dutil.vcxproj || exit /b

@popd
@endlocal