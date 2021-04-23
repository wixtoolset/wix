@setlocal
@pushd %~dp0
@set _C=Release
@if /i "%1"=="debug" set _C=Debug

nuget restore || exit /b

msbuild -t:Test -p:Configuration=%_C% src\test\BurnUnitTest || exit /b

msbuild -p:Configuration=%_C%;Platform=x86 || exit /b
msbuild -p:Configuration=%_C%;Platform=x64 || exit /b
msbuild -p:Configuration=%_C%;Platform=arm64 || exit /b

msbuild -p:Configuration=%_C% -t:PackNative src\stub\stub.vcxproj || exit /b

@popd
@endlocal