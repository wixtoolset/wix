@setlocal
@pushd %~dp0

nuget restore || exit /b

msbuild -p:Configuration=Release .\src\test\WixToolsetTest.Core.Native\WixToolsetTest.Core.Native.csproj || exit /b

msbuild -t:Pack -p:Configuration=Release .\src\WixToolset.Core.Native\WixToolset.Core.Native.csproj || exit /b

msbuild -t:Pack -p:Configuration=Release .\src\wixnative\wixnative.vcxproj || exit /b

@popd
@endlocal