@setlocal
@pushd %~dp0

nuget restore || exit /b

msbuild -p:Configuration=Release -t:Restore || exit /b

msbuild -p:Configuration=Release src\test\WixToolsetTest.Dependency\WixToolsetTest.Dependency.csproj || exit /b
dotnet test -c Release --no-build src\test\WixToolsetTest.Dependency || exit /b

msbuild -p:Configuration=Release -t:Pack src\wixext\WixToolset.Dependency.wixext.csproj || exit /b

@popd
@endlocal