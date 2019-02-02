@setlocal
@pushd %~dp0

nuget restore

msbuild -p:Configuration=Release -t:Restore

msbuild -p:Configuration=Release src\test\WixToolsetTest.DirectX\WixToolsetTest.DirectX.csproj

msbuild -p:Configuration=Release -t:Pack src\wixext\WixToolset.DirectX.wixext.csproj

@popd
@endlocal