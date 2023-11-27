@echo off

setlocal
pushd %~dp0

set _NUGET_CACHE=%USERPROFILE%\.nuget\packages
if "%NUGET_PACKAGES%" NEQ "" set _NUGET_CACHE=%NUGET_PACKAGES%

echo Cleaning...

if exist ..\build rd /s/q ..\build
if exist ..\packages rd /s/q ..\packages

if exist Directory.Packages.props (del Directory.Packages.props)
if exist global.json (del global.json)

if exist ..\Directory.Packages.props (del ..\Directory.Packages.props)
if exist ..\global.json (del ..\global.json)

if exist "%_NUGET_CACHE%\wixinternal.basebuildtasks.sources" rd /s/q "%_NUGET_CACHE%\wixinternal.basebuildtasks.sources"
if exist "%_NUGET_CACHE%\wixinternal.testsupport" rd /s/q "%_NUGET_CACHE%\wixinternal.testsupport"
if exist "%_NUGET_CACHE%\wixinternal.core.testpackage" rd /s/q "%_NUGET_CACHE%\wixinternal.core.testpackage"
if exist "%_NUGET_CACHE%\wixtoolset.bal.wixext" rd /s/q "%_NUGET_CACHE%\wixtoolset.bal.wixext"
if exist "%_NUGET_CACHE%\wixtoolset.balutil" rd /s/q "%_NUGET_CACHE%\wixtoolset.balutil"
if exist "%_NUGET_CACHE%\wixtoolset.bextutil" rd /s/q "%_NUGET_CACHE%\wixtoolset.bextutil"
if exist "%_NUGET_CACHE%\wixtoolset.bootstrappercore.native" rd /s/q "%_NUGET_CACHE%\wixtoolset.bootstrappercore.native"
if exist "%_NUGET_CACHE%\wixtoolset.burn" rd /s/q "%_NUGET_CACHE%\wixtoolset.burn"
if exist "%_NUGET_CACHE%\wixtoolset.complus.wixext" rd /s/q "%_NUGET_CACHE%\wixtoolset.complus.wixext"
if exist "%_NUGET_CACHE%\wixtoolset.converters" rd /s/q "%_NUGET_CACHE%\wixtoolset.converters"
if exist "%_NUGET_CACHE%\wixtoolset.core" rd /s/q "%_NUGET_CACHE%\wixtoolset.core"
if exist "%_NUGET_CACHE%\wixtoolset.core.burn" rd /s/q "%_NUGET_CACHE%\wixtoolset.core.burn"
if exist "%_NUGET_CACHE%\wixtoolset.core.extensioncache" rd /s/q "%_NUGET_CACHE%\wixtoolset.core.extensioncache"
if exist "%_NUGET_CACHE%\wixtoolset.core.native" rd /s/q "%_NUGET_CACHE%\wixtoolset.core.native"
if exist "%_NUGET_CACHE%\wixtoolset.core.windowsinstaller" rd /s/q "%_NUGET_CACHE%\wixtoolset.core.windowsinstaller"
if exist "%_NUGET_CACHE%\wixtoolset.data" rd /s/q "%_NUGET_CACHE%\wixtoolset.data"
if exist "%_NUGET_CACHE%\wixtoolset.dependency.wixext" rd /s/q "%_NUGET_CACHE%\wixtoolset.dependency.wixext"
if exist "%_NUGET_CACHE%\wixtoolset.directx.wixext" rd /s/q "%_NUGET_CACHE%\wixtoolset.directx.wixext"
if exist "%_NUGET_CACHE%\wixtoolset.dnc.hostgenerator" rd /s/q "%_NUGET_CACHE%\wixtoolset.dnc.hostgenerator"
if exist "%_NUGET_CACHE%\wixtoolset.dtf.compression" rd /s/q "%_NUGET_CACHE%\wixtoolset.dtf.compression"
if exist "%_NUGET_CACHE%\wixtoolset.dtf.compression.cab" rd /s/q "%_NUGET_CACHE%\wixtoolset.dtf.compression.cab"
if exist "%_NUGET_CACHE%\wixtoolset.dtf.compression.zip" rd /s/q "%_NUGET_CACHE%\wixtoolset.dtf.compression.zip"
if exist "%_NUGET_CACHE%\wixtoolset.dtf.customaction" rd /s/q "%_NUGET_CACHE%\wixtoolset.dtf.customaction"
if exist "%_NUGET_CACHE%\wixtoolset.dtf.resources" rd /s/q "%_NUGET_CACHE%\wixtoolset.dtf.resources"
if exist "%_NUGET_CACHE%\wixtoolset.dtf.windowsinstaller" rd /s/q "%_NUGET_CACHE%\wixtoolset.dtf.windowsinstaller"
if exist "%_NUGET_CACHE%\wixtoolset.dtf.windowsinstaller.linq" rd /s/q "%_NUGET_CACHE%\wixtoolset.dtf.windowsinstaller.linq"
if exist "%_NUGET_CACHE%\wixtoolset.dtf.windowsinstaller.package" rd /s/q "%_NUGET_CACHE%\wixtoolset.dtf.windowsinstaller.package"
if exist "%_NUGET_CACHE%\wixtoolset.dutil" rd /s/q "%_NUGET_CACHE%\wixtoolset.dutil"
if exist "%_NUGET_CACHE%\wixtoolset.extensibility" rd /s/q "%_NUGET_CACHE%\wixtoolset.extensibility"
if exist "%_NUGET_CACHE%\wixtoolset.firewall.wixext" rd /s/q "%_NUGET_CACHE%\wixtoolset.firewall.wixext"
if exist "%_NUGET_CACHE%\wixtoolset.heat" rd /s/q "%_NUGET_CACHE%\wixtoolset.heat"
if exist "%_NUGET_CACHE%\wixtoolset.http.wixext" rd /s/q "%_NUGET_CACHE%\wixtoolset.http.wixext"
if exist "%_NUGET_CACHE%\wixtoolset.iis.wixext" rd /s/q "%_NUGET_CACHE%\wixtoolset.iis.wixext"
if exist "%_NUGET_CACHE%\wixtoolset.mba.core" rd /s/q "%_NUGET_CACHE%\wixtoolset.mba.core"
if exist "%_NUGET_CACHE%\wixtoolset.msmq.wixext" rd /s/q "%_NUGET_CACHE%\wixtoolset.msmq.wixext"
if exist "%_NUGET_CACHE%\wixtoolset.netfx.wixext" rd /s/q "%_NUGET_CACHE%\wixtoolset.netfx.wixext"
if exist "%_NUGET_CACHE%\wixtoolset.powershell.wixext" rd /s/q "%_NUGET_CACHE%\wixtoolset.powershell.wixext"
if exist "%_NUGET_CACHE%\wixtoolset.sdk" rd /s/q "%_NUGET_CACHE%\wixtoolset.sdk"
if exist "%_NUGET_CACHE%\wixtoolset.sql.wixext" rd /s/q "%_NUGET_CACHE%\wixtoolset.sql.wixext"
if exist "%_NUGET_CACHE%\wixtoolset.util.wixext" rd /s/q "%_NUGET_CACHE%\wixtoolset.util.wixext"
if exist "%_NUGET_CACHE%\wixtoolset.ui.wixext" rd /s/q "%_NUGET_CACHE%\wixtoolset.ui.wixext"
if exist "%_NUGET_CACHE%\wixtoolset.versioning" rd /s/q "%_NUGET_CACHE%\wixtoolset.versioning"
if exist "%_NUGET_CACHE%\wixtoolset.visualstudio.wixext" rd /s/q "%_NUGET_CACHE%\wixtoolset.visualstudio.wixext"
if exist "%_NUGET_CACHE%\wixtoolset.wcautil" rd /s/q "%_NUGET_CACHE%\wixtoolset.wcautil"
if exist "%_NUGET_CACHE%\wix" rd /s/q "%_NUGET_CACHE%\wix"

popd
endlocal
