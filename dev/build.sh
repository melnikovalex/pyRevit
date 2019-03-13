#!/bin/bash
clear
LABS='./pyRevitLabs/pyRevitLabs.sln'
DEVENV='/c/Program Files (x86)/Microsoft Visual Studio/2017/Community/Common7/IDE/devenv.exe'

# install dependencies
choco install upx -y &>/dev/null

# start a log file and listen
cat /dev/null > ./buildlog.log
tail -f ./buildlog.log &

echo "cleaning labs"
"$DEVENV" "$LABS" "//Clean" "Debug" "//out" "./buildlog.log"

echo "building labs"
"$DEVENV" "$LABS" "//build" "Debug" "//out" "./buildlog.log"

# clean log file
kill $!
rm -f ./buildlog.log

# build pyrevit cli auto complete helper
echo "building autocomplete helper binary"
rm -f ../bin/pyrevit-complete.exe
go get github.com/posener/complete/gocomplete
go build -o=../bin/pyrevit-complete.exe ./utils/pyrevit-complete.go
upx ../bin/pyrevit-complete.exe

# pyinstaller --icon=./pyRevitLabs/pyRevitManager/pyRevit\ CLI.ico \
#             --distpath=../bin \
#             --onefile \
#             ./utils/pyrevit-fileinfo.py
# rm -rf ./build
# rm -rf ./dist
# rm -rf ./utils/__pycache__

# build pyrevit fileinfo getter
echo "building fileinfo helper binary"
rm -f ../bin/pyrevit-fileinfo.exe
go build -o=../bin/pyrevit-fileinfo.exe ./utils/pyrevit-fileinfo.go
upx ../bin/pyrevit-fileinfo.exe