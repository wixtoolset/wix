@setlocal
@pushd %~dp0

nuget restore

msbuild -p:Configuration=Release -t:Restore

msbuild -p:Configuration=Release src\test\WixToolsetTest.Util\WixToolsetTest.Util.csproj

msbuild -p:Configuration=Release -t:Pack src\wixext\WixToolset.Util.wixext.csproj

msbuild -p:Configuration=Release src\test\WixToolsetTest.Util\WixToolsetTest.Util.csproj
dotnet test -c Release --no-build src\test\WixToolsetTest.Util

@popd
@endlocal