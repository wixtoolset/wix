@setlocal
@pushd %~dp0

nuget restore || exit /b

msbuild -p:Configuration=Release;Platform=x86;PlatformToolset=v140 || exit /b
msbuild -p:Configuration=Release;Platform=x64;PlatformToolset=v140 || exit /b

msbuild -p:Configuration=Release;Platform=x86;PlatformToolset=v141 || exit /b
msbuild -p:Configuration=Release;Platform=x64;PlatformToolset=v141 || exit /b
msbuild -p:Configuration=Release;Platform=ARM64;PlatformToolset=v141 || exit /b

msbuild -p:Configuration=Release;Platform=x86;PlatformToolset=v142 || exit /b
msbuild -p:Configuration=Release;Platform=x64;PlatformToolset=v142 || exit /b
msbuild -p:Configuration=Release;Platform=ARM64;PlatformToolset=v142 || exit /b

msbuild -p:Configuration=Release -t:PackNativeNuget src\wcautil\wcautil.vcxproj || exit /b

@popd
@endlocal