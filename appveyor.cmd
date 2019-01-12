@setlocal
@pushd %~dp0

nuget restore

msbuild -p:Configuration=Release -t:Restore

msbuild -p:Configuration=Release src\test\WixToolsetTest.UI\WixToolsetTest.UI.csproj

msbuild -p:Configuration=Release -t:Pack src\wixext\WixToolset.UI.wixext.csproj

@popd
@endlocal