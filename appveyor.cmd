@setlocal
@pushd %~dp0

nuget restore

msbuild -p:Configuration=Release .\src\test\WixToolsetTest.Core.Native\WixToolsetTest.Core.Native.csproj

msbuild -t:Pack -p:Configuration=Release .\src\WixToolset.Core.Native\WixToolset.Core.Native.csproj

msbuild -t:Pack -p:Configuration=Release .\src\wixnative\wixnative.vcxproj

@popd
@endlocal