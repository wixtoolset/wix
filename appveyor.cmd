@setlocal
@pushd %~dp0

nuget restore || exit /b

msbuild -t:Test -p:Configuration=Release src\test\DUtilUnitTest || exit /b

msbuild -p:Configuration=Release;Platform=x86;PlatformToolset=v140 || exit /b
msbuild -p:Configuration=Release;Platform=x64;PlatformToolset=v140 || exit /b

msbuild -p:Configuration=Release;Platform=x86;PlatformToolset=v141 || exit /b
msbuild -p:Configuration=Release;Platform=x64;PlatformToolset=v141 || exit /b
msbuild -p:Configuration=Release;Platform=ARM64;PlatformToolset=v141 || exit /b

msbuild -p:Configuration=Release;Platform=x86;PlatformToolset=v142 || exit /b
msbuild -p:Configuration=Release;Platform=x64;PlatformToolset=v142 || exit /b
msbuild -p:Configuration=Release;Platform=ARM64;PlatformToolset=v142 || exit /b

msbuild -p:Configuration=Release -t:PackNativeNuget src\dutil\dutil.vcxproj || exit /b

@popd
@endlocal