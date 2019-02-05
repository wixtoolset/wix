@setlocal
@pushd %~dp0

nuget restore

msbuild -p:Configuration=Release -t:Restore

msbuild -p:Configuration=Release -p:Platform=Win32 src\ca\complusca.vcxproj
msbuild -p:Configuration=Release -p:Platform=x64 src\ca\complusca.vcxproj

msbuild -p:Configuration=Release src\test\WixToolsetTest.ComPlus\WixToolsetTest.ComPlus.csproj

msbuild -p:Configuration=Release -t:Pack src\wixext\WixToolset.ComPlus.wixext.csproj

@popd
@endlocal