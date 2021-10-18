@setlocal
@pushd %~dp0

@set _C=Debug
:parse_args
@if /i "%1"=="release" set _C=Release
@if not "%1"=="" shift & goto parse_args

@echo Building dtf %_C%

:: dtf

nuget restore || exit /b

msbuild -t:Build -p:Configuration=%_C% -m -v:m -nr:false || exit /b

msbuild -t:Pack -p:Configuration=%_C% WixToolset.Dtf.Compression -v:m || exit /b
msbuild -t:Pack -p:Configuration=%_C% WixToolset.Dtf.Compression.Cab -v:m || exit /b
msbuild -t:Pack -p:Configuration=%_C% WixToolset.Dtf.Compression.Zip -v:m || exit /b
msbuild -t:Pack -p:Configuration=%_C% WixToolset.Dtf.Resources -v:m || exit /b
msbuild -t:Pack -p:Configuration=%_C% WixToolset.Dtf.WindowsInstaller -v:m || exit /b
msbuild -t:Pack -p:Configuration=%_C% WixToolset.Dtf.WindowsInstaller.Linq -v:m || exit /b
msbuild -t:Pack -p:Configuration=%_C% WixToolset.Dtf.WindowsInstaller.Package -v:m || exit /b

@popd
@endlocal
