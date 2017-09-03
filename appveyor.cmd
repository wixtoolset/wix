@setlocal
@pushd %~dp0

msbuild -p:Configuration=Release;Platform=Win32 -t:PackNativeNuget src\winterop\winterop.vcxproj
msbuild -p:Configuration=Release;Platform=x64 -t:PackNativeNuget src\winterop\winterop.vcxproj

dotnet pack -c Release .\src\WixToolset.Core.Native\WixToolset.Core.Native.csproj

@popd
@endlocal