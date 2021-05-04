@setlocal
@pushd %~dp0

nuget restore || exit /b

msbuild -p:Configuration=Release -t:Restore || exit /b

msbuild -p:Configuration=Release src\test\WixToolsetTest.UI\WixToolsetTest.UI.csproj || exit /b
dotnet test -c Release --no-build src\test\WixToolsetTest.UI || exit /b

msbuild -p:Configuration=Release -t:Pack src\wixext\WixToolset.UI.wixext.csproj || exit /b

@popd
@endlocal