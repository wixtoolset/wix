@setlocal
@pushd %~dp0

nuget restore

msbuild -p:Configuration=Release -t:Restore

msbuild -p:Configuration=Release src\test\WixToolsetTest.Msmq\WixToolsetTest.Msmq.csproj

msbuild -p:Configuration=Release -t:Pack src\wixext\WixToolset.Msmq.wixext.csproj

@popd
@endlocal