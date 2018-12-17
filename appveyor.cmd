@setlocal
@pushd %~dp0

nuget restore

msbuild -p:Configuration=Release -t:Restore

msbuild -p:Configuration=Release src\test\WixToolsetTest.Iis\WixToolsetTest.Iis.csproj

msbuild -p:Configuration=Release -t:Pack src\wixext\WixToolset.Iis.wixext.csproj

@popd
@endlocal