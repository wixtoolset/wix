@setlocal
@pushd %~dp0

nuget restore || exit /b

msbuild -p:Configuration=Release -t:Restore || exit /b

msbuild -p:Configuration=Release src\test\WixToolsetTest.Iis\WixToolsetTest.Iis.csproj || exit /b
dotnet test -c Release --no-build src\test\WixToolsetTest.Iis || exit /b

msbuild -p:Configuration=Release -t:Pack src\wixext\WixToolset.Iis.wixext.csproj || exit /b

@popd
@endlocal