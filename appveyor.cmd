@setlocal
@pushd %~dp0

nuget restore

msbuild -p:Configuration=Release -t:Restore

msbuild -p:Configuration=Release src\test\WixToolsetTest.Sql\WixToolsetTest.Sql.csproj

msbuild -p:Configuration=Release -t:Pack src\wixext\WixToolset.Sql.wixext.csproj

@popd
@endlocal