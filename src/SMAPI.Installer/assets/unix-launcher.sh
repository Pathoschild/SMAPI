#!/usr/bin/env bash

##########
## Initial setup
##########
# move to script's directory
cd "$(dirname "$0")" || exit $?


##########
## Open terminal if needed
##########
# on macOS, make sure we're running in a Terminal
# Besides letting the player see errors/warnings/alerts in the console, this is also needed because
# Steam messes with the PATH.
if [ "$(uname)" == "Darwin" ]; then
    if [ ! -t 1 ]; then # https://stackoverflow.com/q/911168/262123
        # sanity check to make sure we don't have an infinite loop of opening windows
        SKIP_TERMINAL=false
        for argument in "$@"; do
            if [ "$argument" == "--no-reopen-terminal" ]; then
                SKIP_TERMINAL=true
                break
            fi
        done

        # reopen in Terminal if needed
        # https://stackoverflow.com/a/29511052/262123
        if [ "$SKIP_TERMINAL" == "false" ]; then
            echo "Reopening in the Terminal app..."
            echo "\"$0\" $@ --no-reopen-terminal" > /tmp/open-smapi-terminal.sh
            chmod +x /tmp/open-smapi-terminal.sh
            cat /tmp/open-smapi-terminal.sh
            open -W -a Terminal /tmp/open-smapi-terminal.sh
            rm /tmp/open-smapi-terminal.sh
            exit 0
        fi
    fi
fi


##########
## Validate assumptions
##########
# script must be run from the game folder
if [ ! -f "Stardew Valley.dll" ]; then
    echo "Oops! SMAPI must be placed in the Stardew Valley game folder.\nSee instructions: https://stardewvalleywiki.com/Modding:Player_Guide";
    read
    exit 1
fi

# .NET 5 must be installed
if ! command -v dotnet >/dev/null 2>&1; then
    echo "Oops! You must have .NET 5 installed to use SMAPI: https://dotnet.microsoft.com/download";
    read
    exit 1
fi


##########
## Launch SMAPI
##########
# macOS
if [ "$(uname)" == "Darwin" ]; then
    # fix "DllNotFoundException: libgdiplus.dylib" errors when loading images in SMAPI
    if [ -f libgdiplus.dylib ]; then
        rm libgdiplus.dylib
    fi
    if [ -f /Library/Frameworks/Mono.framework/Versions/Current/lib/libgdiplus.dylib ]; then
        ln -s /Library/Frameworks/Mono.framework/Versions/Current/lib/libgdiplus.dylib libgdiplus.dylib
    fi

    # launch SMAPI
    dotnet StardewModdingAPI.dll "$@"

# Linux
else
    # choose binary file to launch
    LAUNCH_FILE="./StardewModdingAPI"
    export LAUNCH_FILE

    # select terminal (prefer xterm for best compatibility, then known supported terminals)
    for terminal in xterm gnome-terminal kitty terminator xfce4-terminal konsole terminal termite alacritty mate-terminal x-terminal-emulator; do
        if command -v "$terminal" 2>/dev/null; then
            export TERMINAL_NAME=$terminal
            break;
        fi
    done

    # find the true shell behind x-terminal-emulator
    if [ "$TERMINAL_NAME" = "x-terminal-emulator" ]; then
        export TERMINAL_NAME="$(basename "$(readlink -f $(command -v x-terminal-emulator))")"
    fi

    # run in selected terminal and account for quirks
    export TERMINAL_PATH="$(command -v $TERMINAL_NAME)"
    if [ -x $TERMINAL_PATH ]; then
        case $TERMINAL_NAME in
            terminal|termite)
                # consumes only one argument after -e
                # options containing space characters are unsupported
                exec $TERMINAL_NAME -e "env TERM=xterm $LAUNCH_FILE $@"
                ;;

            xterm|konsole|alacritty)
                # consumes all arguments after -e
                exec $TERMINAL_NAME -e env TERM=xterm $LAUNCH_FILE "$@"
                ;;

            terminator|xfce4-terminal|mate-terminal)
                # consumes all arguments after -x
                exec $TERMINAL_NAME -x env TERM=xterm $LAUNCH_FILE "$@"
                ;;

            gnome-terminal)
                # consumes all arguments after --
                exec $TERMINAL_NAME -- env TERM=xterm $LAUNCH_FILE "$@"
                ;;

            kitty)
                # consumes all trailing arguments
                exec $TERMINAL_NAME env TERM=xterm $LAUNCH_FILE "$@"
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
fi
