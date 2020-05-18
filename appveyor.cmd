@setlocal
@pushd %~dp0

nuget restore

msbuild -p:Configuration=Release -t:Restore

msbuild -p:Configuration=Release src\test\WixToolsetTest.Firewall\WixToolsetTest.Firewall.csproj

msbuild -p:Configuration=Release -t:Pack src\wixext\WixToolset.Firewall.wixext.csproj

msbuild -p:Configuration=Release src\test\WixToolsetTest.Firewall\WixToolsetTest.Firewall.csproj
dotnet test -c Release --no-build src\test\WixToolsetTest.Firewall

@popd
@endlocal