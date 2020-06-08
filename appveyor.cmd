@setlocal
@pushd %~dp0

nuget restore

msbuild -p:Configuration=Release -t:Restore

msbuild -p:Configuration=Release

msbuild -p:Configuration=Release -t:Pack src\wixext\WixToolset.Bal.wixext.csproj
msbuild -p:Configuration=Release -t:Pack src\WixToolset.Mba.Host\WixToolset.Mba.Host.csproj

msbuild -p:Configuration=Release src\test\WixToolsetTest.Bal\WixToolsetTest.Bal.csproj
dotnet test -c Release --no-build src\test\WixToolsetTest.Bal

@popd
@endlocal