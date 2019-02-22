#!/bin/bash
cd "./pyRevitLabs/pyRevitManager"
go build ./pyrevit-complete.go
mv ./pyrevit-complete.exe "../../../bin"