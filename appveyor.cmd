@setlocal
@pushd %~dp0

nuget restore || exit /b

msbuild -p:Configuration=Release -t:Restore || exit /b

msbuild -p:Configuration=Release src\test\WixToolsetTest.Netfx\WixToolsetTest.Netfx.csproj || exit /b
dotnet test -c Release --no-build src\test\WixToolsetTest.Netfx || exit /b

msbuild -p:Configuration=Release -t:Pack src\wixext\WixToolset.NetFx.wixext.csproj || exit /b

@popd
@endlocal