@setlocal
@pushd %~dp0

nuget restore || exit /b

msbuild -p:Configuration=Release -t:Restore || exit /b

msbuild -p:Configuration=Release -p:Platform=Win32 src\ca\complusca.vcxproj || exit /b
msbuild -p:Configuration=Release -p:Platform=x64 src\ca\complusca.vcxproj || exit /b

msbuild -p:Configuration=Release src\test\WixToolsetTest.ComPlus\WixToolsetTest.ComPlus.csproj || exit /b
dotnet test -c Release --no-build src\test\WixToolsetTest.ComPlus || exit /b

msbuild -p:Configuration=Release -t:Pack src\wixext\WixToolset.ComPlus.wixext.csproj || exit /b

@popd
@endlocal