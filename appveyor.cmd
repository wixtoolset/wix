@setlocal
@pushd %~dp0

nuget restore || exit /b

msbuild -p:Configuration=Release -Restore || exit /b

dotnet test -c Release --no-build src\test\WixToolsetTest.Util || exit /b

msbuild -p:Configuration=Release -p:NoBuild=true -t:Pack src\wixext\WixToolset.Util.wixext.csproj || exit /b

@popd
@endlocal
