#!/bin/bash

# pyinstaller --icon=./pyRevitLabs/pyRevitManager/pyRevit\ CLI.ico \
#             --distpath=../bin \
#             --onefile \
#             ./utils/pyrevit-fileinfo.py
# rm -rf ./build
# rm -rf ./dist
# rm -rf ./utils/__pycache__

# build pyrevit cli auto complete helper
go build -o=../bin/pyrevit-complete.exe ./pyRevitLabs/pyRevitManager/pyrevit-complete.go
echo "Built pyrevit-complete.go"
upx ../bin/pyrevit-complete.exe

# build pyrevit fileinfo getter
go build -o=../bin/pyrevit-fileinfo.exe ./utils/pyrevit-fileinfo.go
echo "Built pyrevit-fileinfo.go"
upx ../bin/pyrevit-fileinfo.exe