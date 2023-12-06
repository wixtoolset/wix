@setlocal
@pushd %~dp0

@set _C=Debug
:parse_args
@if /i "%1"=="release" set _C=Release
@if not "%1"=="" shift & goto parse_args

@echo Building dtf %_C%

msbuild -Restore SfxCA\sfxca_t.proj -p:Configuration=%_C% -tl -nologo -m -warnaserror -bl:..\..\build\logs\dtf_sfxca.binlog || exit /b

msbuild -Restore -t:Pack dtf.sln -p:Configuration=%_C% -tl -nologo -m -warnaserror -bl:..\..\build\logs\dtf_build.binlog || exit /b

@popd
@endlocal
