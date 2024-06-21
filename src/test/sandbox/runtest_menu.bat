@setlocal EnableDelayedExpansion
@echo off

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
set index=0
if not "%MsVsMonPath%"=="" (
	echo [!index!] Run Remote Debugger SandboxIP=%IPAddr%
	set "option[!index!]=%MsVsMonPath%"
)

for /f %%i in ('where /R c:\build runtests.cmd') do (
  set /A "index+=1"
  echo [!index!] %%i
  set "option[!index!]=%%i!"
)
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
for %%a in (%TestIs%) do set FileDir=%%~dpa
for %%a in (%TestIs%) do set FileName=%%~nxa
start /D %FileDir% %FileName%
if %SelectTest%==0 (
goto TestSelect
)

:End
@endlocal