@echo off

setlocal
pushd %~dp0
set _NUGET_CACHE=%USERPROFILE%\.nuget\packages
echo Cleaning...

if exist ..\build rd /s/q ..\build
if exist ..\packages rd /s/q ..\packages

if exist Directory.Packages.props (del Directory.Packages.props)
if exist global.json (del global.json)

if exist ..\Directory.Packages.props (del ..\Directory.Packages.props)
if exist ..\global.json (del ..\global.json)

if exist "%_NUGET_CACHE%\wixbuildtools.testsupport" rd /s/q "%_NUGET_CACHE%\wixbuildtools.testsupport"
if exist "%_NUGET_CACHE%\wixtoolset.bal.wixext" rd /s/q "%_NUGET_CACHE%\wixtoolset.bal.wixext"
if exist "%_NUGET_CACHE%\wixtoolset.balutil" rd /s/q "%_NUGET_CACHE%\wixtoolset.balutil"
if exist "%_NUGET_CACHE%\wixtoolset.bextutil" rd /s/q "%_NUGET_CACHE%\wixtoolset.bextutil"
if exist "%_NUGET_CACHE%\wixtoolset.bootstrappercore.native" rd /s/q "%_NUGET_CACHE%\wixtoolset.bootstrappercore.native"
if exist "%_NUGET_CACHE%\wixtoolset.burn" rd /s/q "%_NUGET_CACHE%\wixtoolset.burn"
if exist "%_NUGET_CACHE%\wixtoolset.core" rd /s/q "%_NUGET_CACHE%\wixtoolset.core"
if exist "%_NUGET_CACHE%\wixtoolset.core.burn" rd /s/q "%_NUGET_CACHE%\wixtoolset.core.burn"
if exist "%_NUGET_CACHE%\wixtoolset.core.native" rd /s/q "%_NUGET_CACHE%\wixtoolset.core.native"
if exist "%_NUGET_CACHE%\wixtoolset.core.testpackage" rd /s/q "%_NUGET_CACHE%\wixtoolset.core.testpackage"
if exist "%_NUGET_CACHE%\wixtoolset.core.windowsinstaller" rd /s/q "%_NUGET_CACHE%\wixtoolset.core.windowsinstaller"
if exist "%_NUGET_CACHE%\wixtoolset.data" rd /s/q "%_NUGET_CACHE%\wixtoolset.data"
if exist "%_NUGET_CACHE%\wixtoolset.dependency.wixext" rd /s/q "%_NUGET_CACHE%\wixtoolset.dependency.wixext"
if exist "%_NUGET_CACHE%\wixtoolset.dnc.hostgenerator" rd /s/q "%_NUGET_CACHE%\wixtoolset.dnc.hostgenerator"
if exist "%_NUGET_CACHE%\wixtoolset.dtf.compression" rd /s/q "%_NUGET_CACHE%\wixtoolset.dtf.compression"
if exist "%_NUGET_CACHE%\wixtoolset.dtf.compression.cab" rd /s/q "%_NUGET_CACHE%\wixtoolset.dtf.compression.cab"
if exist "%_NUGET_CACHE%\wixtoolset.dtf.customaction" rd /s/q "%_NUGET_CACHE%\wixtoolset.dtf.customaction"
if exist "%_NUGET_CACHE%\wixtoolset.dtf.resources" rd /s/q "%_NUGET_CACHE%\wixtoolset.dtf.resources"
if exist "%_NUGET_CACHE%\wixtoolset.dtf.windowsinstaller" rd /s/q "%_NUGET_CACHE%\wixtoolset.dtf.windowsinstaller"
if exist "%_NUGET_CACHE%\wixtoolset.dutil" rd /s/q "%_NUGET_CACHE%\wixtoolset.dutil"
if exist "%_NUGET_CACHE%\wixtoolset.extensibility" rd /s/q "%_NUGET_CACHE%\wixtoolset.extensibility"
if exist "%_NUGET_CACHE%\wixtoolset.mba.core" rd /s/q "%_NUGET_CACHE%\wixtoolset.mba.core"
if exist "%_NUGET_CACHE%\wixtoolset.netfx.wixext" rd /s/q "%_NUGET_CACHE%\wixtoolset.netfx.wixext"
if exist "%_NUGET_CACHE%\wixtoolset.sdk" rd /s/q "%_NUGET_CACHE%\wixtoolset.sdk"
if exist "%_NUGET_CACHE%\wixtoolset.util.wixext" rd /s/q "%_NUGET_CACHE%\wixtoolset.util.wixext"
if exist "%_NUGET_CACHE%\wixtoolset.ui.wixext" rd /s/q "%_NUGET_CACHE%\wixtoolset.ui.wixext"
if exist "%_NUGET_CACHE%\wixtoolset.wcautil" rd /s/q "%_NUGET_CACHE%\wixtoolset.wcautil"
if exist "%_NUGET_CACHE%\wix" rd /s/q "%_NUGET_CACHE%\wix"

popd
endlocal
