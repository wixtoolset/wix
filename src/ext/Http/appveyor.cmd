@setlocal
@pushd %~dp0

nuget restore || exit /b

msbuild -p:Configuration=Release -t:Restore || exit /b

msbuild -p:Configuration=Release src\test\WixToolsetTest.Http\WixToolsetTest.Http.csproj || exit /b
dotnet test -c Release --no-build src\test\WixToolsetTest.Http || exit /b

msbuild -p:Configuration=Release -t:Pack src\wixext\WixToolset.Http.wixext.csproj || exit /b

@popd
@endlocal