@setlocal
@pushd %~dp0

nuget restore || exit /b

msbuild -p:Configuration=Release -t:Restore || exit /b

msbuild -p:Configuration=Release src\test\WixToolsetTest.Firewall\WixToolsetTest.Firewall.csproj || exit /b
dotnet test -c Release --no-build src\test\WixToolsetTest.Firewall || exit /b

msbuild -p:Configuration=Release -t:Pack src\wixext\WixToolset.Firewall.wixext.csproj || exit /b

@popd
@endlocal