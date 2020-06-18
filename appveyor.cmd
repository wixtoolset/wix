@setlocal
@pushd %~dp0
@set _C=Release

nuget restore

msbuild -p:Configuration=%_C%;Platform=x86;PlatformToolset=v140

msbuild -p:Configuration=%_C%;Platform=x86;PlatformToolset=v141

@rem msbuild -t:VSTest -p:Configuration=%_C% src\test\WixToolsetTest.Mba.Core\WixToolsetTest.Mba.Core.csproj

msbuild -t:Pack -p:Configuration=%_C% src\balutil\balutil.vcxproj
msbuild -t:Pack -p:Configuration=%_C% src\bextutil\bextutil.vcxproj
msbuild -t:Pack -p:Configuration=%_C% src\WixToolset.Mba.Core\WixToolset.Mba.Core.csproj
msbuild -t:Pack -p:Configuration=%_C% src\mbanative\mbanative.vcxproj

@popd
@endlocal