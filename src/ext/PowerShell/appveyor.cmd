@setlocal
@pushd %~dp0

nuget restore

msbuild -p:Configuration=Release -t:Restore

msbuild -p:Configuration=Release src\test\WixToolsetTest.PowerShell\WixToolsetTest.PowerShell.csproj

msbuild -p:Configuration=Release -t:Pack src\wixext\WixToolset.PowerShell.wixext.csproj

@popd
@endlocal