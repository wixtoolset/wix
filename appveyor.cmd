@setlocal
@pushd %~dp0

nuget restore

msbuild -p:Configuration=Release -t:Restore

msbuild -p:Configuration=Release src\test\WixToolsetTest.Dependency\WixToolsetTest.Dependency.csproj

msbuild -p:Configuration=Release -t:Pack src\wixext\WixToolset.Dependency.wixext.csproj

@popd
@endlocal