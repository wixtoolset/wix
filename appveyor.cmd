@setlocal
@pushd %~dp0

nuget restore || exit /b

msbuild -p:Configuration=Release -t:Restore || exit /b

msbuild -p:Configuration=Release src\test\WixToolsetTest.Sql\WixToolsetTest.Sql.csproj || exit /b
dotnet test -c Release --no-build src\test\WixToolsetTest.Sql || exit /b

msbuild -p:Configuration=Release -t:Pack src\wixext\WixToolset.Sql.wixext.csproj || exit /b

@popd
@endlocal