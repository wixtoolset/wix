@setlocal
@pushd %~dp0

nuget restore

msbuild -p:Configuration=Release -t:Restore

msbuild -p:Configuration=Release src\test\WixToolsetTest.Http\WixToolsetTest.Http.csproj

msbuild -p:Configuration=Release -t:Pack src\wixext\WixToolset.Http.wixext.csproj

@popd
@endlocal