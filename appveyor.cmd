@setlocal
@pushd %~dp0

nuget restore

msbuild -p:Configuration=Release -t:Restore

msbuild -p:Configuration=Release

msbuild -p:Configuration=Release -t:Pack src\wixext\WixToolset.Bal.wixext.csproj
msbuild -p:Configuration=Release -t:Pack src\WixToolset.Mba.Host\WixToolset.Mba.Host.csproj

@popd
@endlocal