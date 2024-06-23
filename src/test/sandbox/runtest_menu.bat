@setlocal EnableDelayedExpansion
@echo off

REM We look to see if there's a debugger available in the injected sandbox folder
REM from the host.  If there is, we'll show it as a launch option (later) for the
REM user.
REM If we do offer the debugger, we better show the IP address too, since it seems
REM to change for each invocation of the Sandbox environment
for /f "tokens=2 delims=[]" %%a in ('ping -n 1 -4 ""') do set IPAddr=%%a
@if %PROCESSOR_ARCHITECTURE%=="ARM64" (
	@if exist C:\sandbox\Debugger\ARM64\msvsmon.exe (
		set MsVsMonPath=C:\sandbox\Debugger\ARM64\msvsmon.exe
	)
) else (
	@if exist C:\sandbox\Debugger\x64\msvsmon.exe (
		set MsVsMonPath=C:\sandbox\Debugger\x64\msvsmon.exe
	)
)

:TestSelect
cls
REM Show the test select menu

REM We start with an entry for the Debugger if available
set index=0
if not "%MsVsMonPath%"=="" (
	echo [!index!] Run Remote Debugger [SandboxIP=%IPAddr%]
	set "option[!index!]=%MsVsMonPath%"
)

REM And then for each 'runtests.cmd' file we find
for /f %%i in ('where /R c:\build runtests.cmd') do (
  set /A "index+=1"
  echo [!index!] %%i
  set "option[!index!]=%%i!"
)

REM and finally an option to quit the menu
echo [q] Quit

set /P "SelectTest=Please Choose The Test You Want To Execute: "
if "%SelectTest%"=="q" Goto End
if defined option[%SelectTest%] Goto TestSet

:TestError
cls
Echo ERROR: Invalid Test Selected!!
pause
goto TestSelect

:TestSet
set "TestIs=!option[%SelectTest%]!"
if not "%SelectTest%"=="0" (
	REM for the non-Debugger options, we want to get the basepath
	REM the file name, and a TestName component..
	for %%a in (%TestIs%) do set FileDir=%%~dpa
	for %%b in (%TestIs%) do set FileName=%%~nxb

	REM since we start from C:\build, the 3rd token in '\' is
	REM the 'IntegrationMsi' naming that looks good for a TestName
	for /f "tokens=3 delims=\" %%c in ("%TestIs%") do (
		set TestName=%%c
	)

	REM We just start these as a separate window
	REM and use cmd.exe /K to keep it around after execution has finished
	start "!TestName!" /D "!FileDir!" cmd.exe /K !FileName!
) else (
	start !TestIs!
)
goto TestSelect

:End
@endlocal