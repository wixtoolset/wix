@setlocal
@pushd %~dp0

md ..\build\artifacts

msbuild -Restore internal\SetBuildNumber\SetBuildNumber.proj -nologo

@popd
@endlocal
