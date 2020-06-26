@setlocal
@pushd %~dp0

nuget restore || exit /b

msbuild -p:Configuration=Release -t:Restore || exit /b

msbuild -p:Configuration=Release src\test\WixToolsetTest.Util\WixToolsetTest.Util.csproj || exit /b
dotnet test -c Release --no-build src\test\WixToolsetTest.Util || exit /b

msbuild -p:Configuration=Release -t:Pack src\wixext\WixToolset.Util.wixext.csproj || exit /b

@popd
@endlocal
