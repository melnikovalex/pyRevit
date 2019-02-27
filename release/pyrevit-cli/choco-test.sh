#!/bin/bash
choco uninstall pyrevit-cli -y &>/dev/null
choco pack
choco install pyrevit-cli -s "'.;chocolatey'" -y