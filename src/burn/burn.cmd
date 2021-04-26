@setlocal
@pushd %~dp0

@set _C=Debug
:parse_args
@if /i "%1"=="release" set _C=Release& shift
@if not "%1"=="" shift & goto parse_args

@echo Building burn %_C%

:: burn

nuget restore || exit /b

msbuild -t:Test -p:Configuration=%_C% test\BurnUnitTest || exit /b

msbuild -t:Build -p:Configuration=%_C%;Platform=x86 || exit /b
msbuild -t:Build -p:Configuration=%_C%;Platform=x64 || exit /b
msbuild -t:Build -p:Configuration=%_C%;Platform=arm64 || exit /b

msbuild -t:PackNative -p:Configuration=%_C% stub\stub.vcxproj || exit /b

@popd
@endlocal
