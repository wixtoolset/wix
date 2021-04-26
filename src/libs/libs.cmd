@setlocal
@pushd %~dp0

@set _C=Debug
:parse_args
@if /i "%1"=="release" set _C=Release& shift
@if not "%1"=="" shift & goto parse_args

@echo Building libs %_C% using %_N%

:: libs

nuget restore || exit /b

:: dutil

msbuild -t:Test -p:Configuration=%_C% dutil\test\DUtilUnitTest || exit /b

msbuild -p:Configuration=%_C%;Platform=x86;PlatformToolset=v142 dutil\WixToolset.DUtil\dutil.vcxproj || exit /b
msbuild -p:Configuration=%_C%;Platform=x64;PlatformToolset=v142 dutil\WixToolset.DUtil\dutil.vcxproj || exit /b
msbuild -p:Configuration=%_C%;Platform=ARM64;PlatformToolset=v142 dutil\WixToolset.DUtil\dutil.vcxproj || exit /b

msbuild -p:Configuration=%_C%;Platform=x86;PlatformToolset=v141 dutil\WixToolset.DUtil\dutil.vcxproj || exit /b
msbuild -p:Configuration=%_C%;Platform=x64;PlatformToolset=v141 dutil\WixToolset.DUtil\dutil.vcxproj || exit /b
msbuild -p:Configuration=%_C%;Platform=ARM64;PlatformToolset=v141 dutil\WixToolset.DUtil\dutil.vcxproj || exit /b

msbuild -p:Configuration=%_C%;Platform=x86;PlatformToolset=v140 dutil\WixToolset.DUtil\dutil.vcxproj || exit /b
msbuild -p:Configuration=%_C%;Platform=x64;PlatformToolset=v140 dutil\WixToolset.DUtil\dutil.vcxproj || exit /b

msbuild -p:Configuration=%_C% -t:PackNative dutil\WixToolset.DUtil\dutil.vcxproj || exit /b


:: wcautil

msbuild -p:Configuration=%_C%;Platform=x86;PlatformToolset=v142 wcautil\WixToolset.WcaUtil\wcautil.vcxproj || exit /b
msbuild -p:Configuration=%_C%;Platform=x64;PlatformToolset=v142 wcautil\WixToolset.WcaUtil\wcautil.vcxproj || exit /b
msbuild -p:Configuration=%_C%;Platform=ARM64;PlatformToolset=v142 wcautil\WixToolset.WcaUtil\wcautil.vcxproj || exit /b

msbuild -p:Configuration=%_C%;Platform=x86;PlatformToolset=v141 wcautil\WixToolset.WcaUtil\wcautil.vcxproj || exit /b
msbuild -p:Configuration=%_C%;Platform=x64;PlatformToolset=v141 wcautil\WixToolset.WcaUtil\wcautil.vcxproj || exit /b
msbuild -p:Configuration=%_C%;Platform=ARM64;PlatformToolset=v141 wcautil\WixToolset.WcaUtil\wcautil.vcxproj || exit /b

msbuild -p:Configuration=%_C%;Platform=x86;PlatformToolset=v140 wcautil\WixToolset.WcaUtil\wcautil.vcxproj || exit /b
msbuild -p:Configuration=%_C%;Platform=x64;PlatformToolset=v140 wcautil\WixToolset.WcaUtil\wcautil.vcxproj || exit /b

msbuild -t:PackNative -p:Configuration=%_C% wcautil\WixToolset.WcaUtil\wcautil.vcxproj || exit /b

@popd
@endlocal
