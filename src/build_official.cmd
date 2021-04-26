@setlocal
@pushd %~dp0

@copy nuget_official.config nuget.config

build_all.cmd Release Official

@popd
@endlocal
