#!/usr/bin/env bash

function open_in_terminal { # Checks for a few different terminal emulators to launch the installer through
    if which konsole 2&>1 >/dev/null; then # KDE Konsole
        konsole -e $1
    elif which alacritty 2&>1 >/dev/null; then # Alacritty
        alacritty -e $1
    elif which gnome-terminal 2&>1 >/dev/null; then # GNOME Terminal
        gnome-terminal -- $1
    elif which xterm 2&>1 >/dev/null; then # Xterm
        xterm -e $1
    else # Use notify-send to send a message that none of these terminals were found installed and instruct the user to manually invoke this script through a terminal
        notify-send --app-name="SMAPI Installer" --urgency=critical "Failed to find a terminal to open installer with. Please use a terminal program to open the 'install on Linux.sh' script"
    fi
}

cd "`dirname "$0"`"
if [ -t 0 ]; then # Uses `test` to check if the File Descriptor for Standard Input is valid -- STDIN should be valid if script was invoked through a terminal, and invalid if the file was invoked through a file explorer that didn't wrap it in a terminal emulator
    ./internal/linux/SMAPI.Installer
else
    open_in_terminal ./internal/linux/SMAPI.Installer
fi
