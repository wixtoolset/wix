@setlocal
@pushd %~dp0

nuget restore

msbuild -p:Configuration=Release -t:Restore

msbuild -p:Configuration=Release src\test\WixToolsetTest.DifxApp\WixToolsetTest.DifxApp.csproj

msbuild -p:Configuration=Release -t:Pack src\wixext\WixToolset.DifxApp.wixext.csproj

@popd
@endlocal