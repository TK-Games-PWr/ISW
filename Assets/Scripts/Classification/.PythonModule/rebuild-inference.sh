#!/bin/bash

# Change to the directory where the script is located
cd "$(dirname "$0")" || exit 1

echo "Rebuilding inference from current sources..."
echo ""

# Check for PyInstaller in the local virtual environment
PYINSTALLER="venv/bin/pyinstaller"

if [ ! -f "$PYINSTALLER" ]; then
    # Fallback to checking the system PATH
    if ! command -v pyinstaller >/dev/null 2>&1; then
        echo "PyInstaller not found. Install with: pip install pyinstaller"
        exit 1
    fi
    PYINSTALLER="pyinstaller"
fi

# Run PyInstaller with the specified arguments
"$PYINSTALLER" --noconfirm --onefile \
    --runtime-hook runtime_hook_utf8.py \
    --hidden-import sklearn.tree \
    --hidden-import sklearn.tree._classes \
    --hidden-import sklearn.ensemble \
    --collect-submodules sklearn \
    --name inference \
    inference.py

# Check if the build command was successful
if [ $? -ne 0 ]; then
    echo "Build failed."
    exit 1
fi

echo "Build Complete. Restart Play mode in Unity."
