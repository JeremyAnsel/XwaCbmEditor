@echo off
setlocal

cd "%~dp0"

For %%a in (
"XwaCbmEditor\bin\Release\*.dll"
"XwaCbmEditor\bin\Release\*.exe"
"XwaCbmEditor\bin\Release\*.config"
) do (
xcopy /s /d "%%~a" dist\
)

For %%a in (
"XwaCbmExplorer\bin\Release\*.dll"
"XwaCbmExplorer\bin\Release\*.exe"
"XwaCbmExplorer\bin\Release\*.config"
) do (
xcopy /s /d "%%~a" dist\
)
