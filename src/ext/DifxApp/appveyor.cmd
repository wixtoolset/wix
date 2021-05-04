@setlocal
@pushd %~dp0

nuget restore || exit /b

msbuild -p:Configuration=Release -t:Restore || exit /b

msbuild -p:Configuration=Release src\test\WixToolsetTest.DifxApp\WixToolsetTest.DifxApp.csproj || exit /b
dotnet test -c Release --no-build src\test\WixToolsetTest.DifxApp || exit /b

msbuild -p:Configuration=Release -t:Pack src\wixext\WixToolset.DifxApp.wixext.csproj || exit /b

@popd
@endlocal