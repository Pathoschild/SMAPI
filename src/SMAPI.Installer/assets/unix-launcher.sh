#!/usr/bin/env bash

##########
## Initial setup
##########
# move to script's directory
cd "$(dirname "$0")" || exit $?

# Whether to avoid opening a separate terminal window, and avoid logging anything to the console.
# This isn't recommended since you won't see errors, warnings, and update alerts.
SKIP_TERMINAL=false

# Whether to avoid opening a separate terminal, but still send the usual log output to the console.
USE_CURRENT_SHELL=false


##########
## Read environment variables
##########
if [ "$SMAPI_NO_TERMINAL" == "true" ]; then
    SKIP_TERMINAL=true
fi
if [ "$SMAPI_USE_CURRENT_SHELL" == "true" ]; then
    USE_CURRENT_SHELL=true
fi


##########
## Read command-line arguments
##########
while [ "$#" -gt 0 ]; do
    case "$1" in
        --skip-terminal ) SKIP_TERMINAL=true; shift ;;
        --use-current-shell ) USE_CURRENT_SHELL=true; shift ;;
        -- ) shift; break ;;
        * ) shift ;;
    esac
done

if [ "$SKIP_TERMINAL" == "true" ]; then
    USE_CURRENT_SHELL=true
fi


##########
## Open terminal if needed
##########
# on macOS, make sure we're running in a Terminal
# Besides letting the player see errors/warnings/alerts in the console, this is also needed because
# Steam messes with the PATH.
if [ "$(uname)" == "Darwin" ]; then
    if [ ! -t 1 ]; then # not open in Terminal (https://stackoverflow.com/q/911168/262123)
        # reopen in Terminal if needed
        # https://stackoverflow.com/a/29511052/262123
        if [ "$USE_CURRENT_SHELL" == "false" ]; then
            echo "Reopening in the Terminal app..."
            echo '#!/bin/sh' > /tmp/open-smapi-terminal.command
            echo "\"$0\" $@ --use-current-shell" >> /tmp/open-smapi-terminal.command
            chmod +x /tmp/open-smapi-terminal.command
            cat /tmp/open-smapi-terminal.command
            # Check if iTerm2 is installed by attempting to locate the iTerm application
            if [ -d "/Applications/iTerm.app" ]; then
                echo "Opening in iTerm2..."
                # Open the script in iTerm2
                open -a "/Applications/iTerm.app" /tmp/open-smapi-terminal.command
            else
                echo "iTerm2 not found. Opening in the default Terminal app..."
                # Fall back to the default Terminal if iTerm2 is not found
                open -W /tmp/open-smapi-terminal.command
            fi
            # Remove the temporary script after a delay
            (sleep 10; rm /tmp/open-smapi-terminal.command)
            exit 0
        fi
    fi
fi


##########
## Validate assumptions
##########
# script must be run from the game folder
if [ ! -f "Stardew Valley.dll" ]; then
    printf "Oops! SMAPI must be placed in the Stardew Valley game folder.\nSee instructions: https://stardewvalleywiki.com/Modding:Player_Guide";
    read -r
    exit 1
fi


##########
## Launch SMAPI
##########
# macOS
if [ "$(uname)" == "Darwin" ]; then
    ./StardewModdingAPI "$@"

# Linux
else
    # choose binary file to launch
    LAUNCH_FILE="./StardewModdingAPI"
    export LAUNCH_FILE

    # run in terminal
    if [ "$USE_CURRENT_SHELL" == "false" ]; then
        # select terminal (prefer xterm for best compatibility, then known supported terminals)
        for terminal in xterm gnome-terminal kitty terminator xfce4-terminal konsole terminal termite alacritty mate-terminal x-terminal-emulator wezterm; do
            if command -v "$terminal" 2>/dev/null; then
                export TERMINAL_NAME=$terminal
                break;
            fi
        done

        # find the true shell behind x-terminal-emulator
        if [ "$TERMINAL_NAME" = "x-terminal-emulator" ]; then
            TERMINAL_NAME="$(basename "$(readlink -f "$(command -v x-terminal-emulator)")")"
            export TERMINAL_NAME
        fi

        # run in selected terminal and account for quirks
        TERMINAL_PATH="$(command -v "$TERMINAL_NAME")"
        export TERMINAL_PATH
        if [ -x "$TERMINAL_PATH" ]; then
            case $TERMINAL_NAME in
                terminal|termite)
                    # consumes only one argument after -e
                    # options containing space characters are unsupported
                    exec "$TERMINAL_NAME" -e "env TERM=xterm $LAUNCH_FILE $@"
                    ;;

                xterm|konsole|alacritty)
                    # consumes all arguments after -e
                    exec "$TERMINAL_NAME" -e env TERM=xterm $LAUNCH_FILE "$@"
                    ;;

                terminator|xfce4-terminal|mate-terminal)
                    # consumes all arguments after -x
                    exec "$TERMINAL_NAME" -x env TERM=xterm $LAUNCH_FILE "$@"
                    ;;

                gnome-terminal)
                    # consumes all arguments after --
                    exec "$TERMINAL_NAME" -- env TERM=xterm $LAUNCH_FILE "$@"
                    ;;

                wezterm)
                    # consumes all arguments after start --. does not copy working directory automatically, needs --cwd for that.
                    exec "$TERMINAL_NAME" start --cwd "$(pwd)" -- env TERM=xterm $LAUNCH_FILE "$@"
                    ;;

                kitty)
                    # consumes all trailing arguments
                    exec "$TERMINAL_NAME" env TERM=xterm $LAUNCH_FILE "$@"
                    ;;

                *)
                    # If we don't know the terminal, just try to run it in the current shell.
                    # If THAT fails, launch with no output.
                    env TERM=xterm $LAUNCH_FILE "$@"
                    if [ $? -eq 127 ]; then
                        exec $LAUNCH_FILE --no-terminal "$@"
                    fi
            esac

        ## terminal isn't executable; fallback to current shell or no terminal
        else
            echo "The '$TERMINAL_NAME' terminal isn't executable. SMAPI might be running in a sandbox or the system might be misconfigured? Falling back to current shell."
            env TERM=xterm $LAUNCH_FILE "$@"
            if [ $? -eq 127 ]; then
                exec $LAUNCH_FILE --no-terminal "$@"
            fi
        fi

    # explicitly run without terminal
    elif [ "$SKIP_TERMINAL" == "true" ]; then
        exec $LAUNCH_FILE --no-terminal "$@"
    else
        exec $LAUNCH_FILE "$@"
    fi
fi
