@echo off
setlocal
cd /d "%~dp0"

echo Rebuilding inference.exe from current sources...
echo.

set "PYINSTALLER=venv\Scripts\pyinstaller.exe"
if not exist "%PYINSTALLER%" (
    where pyinstaller >nul 2>&1
    if errorlevel 1 (
        echo PyInstaller not found. Install with: pip install pyinstaller
        exit /b 1
    )
    set "PYINSTALLER=pyinstaller"
)

"%PYINSTALLER%" --noconfirm --onefile ^
    --runtime-hook runtime_hook_utf8.py ^
    --hidden-import sklearn.tree ^
    --hidden-import sklearn.tree._classes ^
    --hidden-import sklearn.ensemble ^
    --collect-submodules sklearn ^
    --name inference ^
    inference.py

if errorlevel 1 (
    echo Build failed.
    exit /b 1
)

copy /Y "dist\inference.exe" "inference.exe"
echo.
echo Done. Copied dist\inference.exe to:
echo   %cd%\inference.exe
echo.
echo Restart Play mode in Unity.
endlocal
