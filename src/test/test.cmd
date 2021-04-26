@setlocal
@pushd %~dp0

@set _C=Debug
:parse_args
@if /i "%1"=="release" set _C=Release& shift
@if not "%1"=="" shift & goto parse_args

@echo Build integration tests %_C%

@call burn\test_burn.cmd %_C%

@popd
@endlocal
