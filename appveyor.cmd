@setlocal
@pushd %~dp0

nuget restore || exit /b

msbuild -p:Configuration=Release -t:Restore || exit /b

msbuild -p:Configuration=Release src\test\WixToolsetTest.VisualStudio\WixToolsetTest.VisualStudio.csproj || exit /b
dotnet test -c Release --no-build src\test\WixToolsetTest.VisualStudio || exit /b

msbuild -p:Configuration=Release -t:Pack src\wixext\WixToolset.VisualStudio.wixext.csproj || exit /b

@popd
@endlocal