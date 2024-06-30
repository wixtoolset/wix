@setlocal
@echo off
SET DOTNET_VERSION=8.0
SET SANDBOX_FILES=C:\sandbox

set /p InstallMethod=<%SANDBOX_FILES%\dotnet.cfg

pushd "%TEMP%"
if "%InstallMethod%"=="EXE" goto EXE
if "%InstallMethod%"=="ZIP" goto ZIP
goto ERROR_NO_CONFIG

:ZIP
mkdir "%ProgramFiles%\dotnet"
if exist %SANDBOX_FILES%\assets\dotnet-sdk-64.zip (
	tar -oxzf "%SANDBOX_FILES%\assets\dotnet-sdk-64.zip" -C "%ProgramFiles%\dotnet"
) else (
	goto ERROR_NO_DOTNET
)
if exist %SANDBOX_FILES%\assets\windowsdesktop-runtime-64.zip (
	tar -oxzf "%SANDBOX_FILES%\assets\windowsdesktop-runtime-64.zip" -C "%ProgramFiles%\dotnet"
) else (
	goto ERROR_NO_DOTNET
)
if exist %SANDBOX_FILES%\assets\dotnet-runtime-x86.zip (
	mkdir "%ProgramFiles(x86)%\dotnet"
	tar -oxzf "%SANDBOX_FILES%\assets\dotnet-runtime-x86.zip" -C "%ProgramFiles(x86)%\dotnet"
)
if exist %SANDBOX_FILES%\assets\windowsdesktop-runtime-x86.zip (
	tar -oxzf "%SANDBOX_FILES%\assets\windowsdesktop-runtime-x86.zip" -C "%ProgramFiles(x86)%\dotnet"
)
goto PROCEED

:EXE
if exist %SANDBOX_FILES%\assets\dotnet-sdk-64.exe (
	"%SANDBOX_FILES%\assets\dotnet-sdk-64.exe" /install /quiet /norestart
) else (
	goto ERROR_NO_DOTNET
)
if exist %SANDBOX_FILES%\assets\windowsdesktop-runtime-64.exe (
	"%SANDBOX_FILES%\assets\windowsdesktop-runtime-64.exe" /install /quiet /norestart
) else (
	goto ERROR_NO_DOTNET
)
if exist %SANDBOX_FILES%\assets\windowsdesktop-runtime-x86.exe (
	"%SANDBOX_FILES%\assets\windowsdesktop-runtime-x86.exe" /install /quiet /norestart
)
goto PROCEED

:PROCEED
regedit /s "%SANDBOX_FILES%\sandbox_registry"
endlocal
SETX PATH "%PATH%;%ProgramFiles%\dotnet;%ProgramFiles(x86)%\dotnet" /M
SET PATH=%PATH%;%ProgramFiles%\dotnet;%ProgramFiles(x86)%\dotnet

dotnet nuget locals all --clear
dotnet help

popd
cd c:\build
start "Menu" cmd /c C:\sandbox\runtest_menu.bat
goto END

:ERROR_NO_CONFIG
start "ERROR" CMD /c echo ERROR: Host configuration has not been run, run setup_sandbox.bat first ^& pause
goto END


:ERROR_NO_DOTNET
start "ERROR" CMD /c echo ERROR: Failed to find dotnet install files. Run setup_sandbox.bat again ^& pause

:END
