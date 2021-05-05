@setlocal
@pushd %~dp0

nuget restore || exit /b

msbuild -p:Configuration=Release -t:Restore || exit /b

msbuild -p:Configuration=Release src\test\WixToolsetTest.Msmq\WixToolsetTest.Msmq.csproj || exit /b
dotnet test -c Release --no-build src\test\WixToolsetTest.Msmq || exit /b

msbuild -p:Configuration=Release -t:Pack src\wixext\WixToolset.Msmq.wixext.csproj || exit /b

@popd
@endlocal