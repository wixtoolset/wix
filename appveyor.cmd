@setlocal
@pushd %~dp0

nuget restore

msbuild -p:Configuration=Release;Platform=x86

msbuild -p:Configuration=Release -t:Pack src\stub\stub.vcxproj

@popd
@endlocal