@setlocal
@pushd %~dp0

md ..\build\artifacts

msbuild -Restore -v:m internal\SetBuildNumber\SetBuildNumber.proj

@popd
@endlocal
