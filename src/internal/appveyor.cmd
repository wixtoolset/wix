@setlocal
@pushd %~dp0

nuget restore || exit /b

dotnet pack -c Release src\WixBuildTools.MsgGen\WixBuildTools.MsgGen.csproj || exit /b
dotnet pack -c Release src\WixBuildTools.TestSupport\WixBuildTools.TestSupport.csproj || exit /b
dotnet pack -c Release src\WixBuildTools.XsdGen\WixBuildTools.XsdGen.csproj || exit /b

msbuild -p:Configuration=Release -t:PackNativeNuget src\WixBuildTools.TestSupport.Native\WixBuildTools.TestSupport.Native.vcxproj || exit /b

@popd
@endlocal