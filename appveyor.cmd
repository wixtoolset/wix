@setlocal
@pushd %~dp0

nuget restore || exit /b

msbuild -p:Configuration=Release -Restore || exit /b

dotnet test -c Release --no-build src\test\WixToolsetTest.Netfx || exit /b

msbuild -p:Configuration=Release -p:NoBuild=true -t:Pack src\wixext\WixToolset.NetFx.wixext.csproj || exit /b

@popd
@endlocal