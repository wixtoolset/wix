@setlocal
@pushd %~dp0

nuget restore

msbuild -p:Configuration=Release -t:Restore

msbuild -p:Configuration=Release src\test\WixToolsetTest.NetFx\WixToolsetTest.NetFx.csproj

msbuild -p:Configuration=Release -t:Pack src\wixext\WixToolset.NetFx.wixext.csproj

msbuild -p:Configuration=Release src\test\WixToolsetTest.Netfx\WixToolsetTest.Netfx.csproj
dotnet test -c Release --no-build src\test\WixToolsetTest.Netfx

@popd
@endlocal