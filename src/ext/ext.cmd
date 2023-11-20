@setlocal
@pushd %~dp0

@set _C=Debug
:parse_args
@if /i "%1"=="release" set _C=Release
@if not "%1"=="" shift & goto parse_args

@echo ext build %_C%

:: These extensions must be built in this order.

:: Util
call Util\util.cmd %_C% || exit /b

:: Bal
call Bal\bal.cmd %_C% || exit /b

:: NetFx
call NetFx\netfx.cmd %_C% || exit /b


:: The rest of the extensions are in alphabetical order.

:: ComPlus
call ComPlus\complus.cmd %_C% || exit /b

:: Dependency
call Dependency\dependency.cmd %_C% || exit /b

:: DirectX
call DirectX\directx.cmd %_C% || exit /b

:: Firewall
call Firewall\firewall.cmd %_C% || exit /b

:: Http
call Http\http.cmd %_C% || exit /b

:: Iis
call Iis\iis.cmd %_C% || exit /b

:: Msmq
call Msmq\msmq.cmd %_C% || exit /b

:: PowerShell
call PowerShell\ps.cmd %_C% || exit /b

:: Sql
call Sql\sql.cmd %_C% || exit /b

:: UI
call UI\ui.cmd %_C% || exit /b

:: VisualStudio
call VisualStudio\vs.cmd %_C% || exit /b

@popd
@endlocal
