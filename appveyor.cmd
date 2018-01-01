@setlocal
@pushd %~dp0

nuget restore

msbuild -p:Configuration=Release -t:Restore

msbuild -p:Configuration=Release src\test\WixToolsetTest.NetFx\WixToolsetTest.NetFx.csproj

msbuild -p:Configuration=Release -t:Pack src\wixext\WixToolset.NetFx.wixext.csproj

@popd
@endlocal