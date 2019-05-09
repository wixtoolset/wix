@setlocal
@pushd %~dp0

nuget restore

msbuild -p:Configuration=Release;Platform=x86;PlatformToolset=v140_xp
msbuild -p:Configuration=Release;Platform=x64;PlatformToolset=v140_xp

msbuild -p:Configuration=Release;Platform=x86;PlatformToolset=v141_xp
msbuild -p:Configuration=Release;Platform=x64;PlatformToolset=v141_xp
rem msbuild -p:Configuration=Release;Platform=ARM;PlatformToolset=v141
rem msbuild -p:Configuration=Release;Platform=ARM64;PlatformToolset=v141

msbuild -p:Configuration=Release;Platform=x86;PlatformToolset=v142
msbuild -p:Configuration=Release;Platform=x64;PlatformToolset=v142
rem msbuild -p:Configuration=Release;Platform=ARM;PlatformToolset=v142
rem msbuild -p:Configuration=Release;Platform=ARM64;PlatformToolset=v142

msbuild -p:Configuration=Release -t:PackNativeNuget src\dutil\dutil.vcxproj

@popd
@endlocal