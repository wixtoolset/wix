@setlocal
@pushd %~dp0

nuget restore

msbuild -p:Configuration=Release -t:Restore

msbuild -p:Configuration=Release -t:Pack .\src\wixext\WixToolset.VisualStudio.wixext.csproj

@popd
@endlocal