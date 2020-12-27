@setlocal
@pushd %~dp0

nuget restore || exit /b

msbuild -t:Test -p:Configuration=Release src\test\BurnUnitTest || exit /b

msbuild -p:Configuration=Release;Platform=x86 || exit /b
msbuild -p:Configuration=Release;Platform=x64 || exit /b
msbuild -p:Configuration=Release;Platform=arm64 || exit /b

msbuild -p:Configuration=Release -t:Pack src\stub\stub.vcxproj || exit /b
msbuild -p:Configuration=Release -t:Pack src\WixToolset.BootstrapperCore.Native\WixToolset.BootstrapperCore.Native.proj || exit /b

@popd
@endlocal