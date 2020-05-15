@setlocal
@pushd %~dp0

nuget restore

msbuild -p:Configuration=Release;Platform=x86;PlatformToolset=v140

msbuild -p:Configuration=Release;Platform=x86;PlatformToolset=v141

msbuild -p:Configuration=Release -t:Pack src\balutil\balutil.vcxproj
msbuild -p:Configuration=Release -t:Pack src\bextutil\bextutil.vcxproj
msbuild -p:Configuration=Release -t:Pack src\WixToolset.Mba.Core\WixToolset.Mba.Core.csproj

@popd
@endlocal