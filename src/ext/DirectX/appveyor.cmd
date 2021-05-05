@setlocal
@pushd %~dp0

nuget restore || exit /b

msbuild -p:Configuration=Release -t:Restore || exit /b

msbuild -p:Configuration=Release src\test\WixToolsetTest.DirectX\WixToolsetTest.DirectX.csproj || exit /b
dotnet test -c Release --no-build src\test\WixToolsetTest.DirectX || exit /b

msbuild -p:Configuration=Release -t:Pack src\wixext\WixToolset.DirectX.wixext.csproj || exit /b

@popd
@endlocal