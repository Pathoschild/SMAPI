#!/bin/bash

# Move to script's directory
cd "`dirname "$0"`"

# if $TERM is not set to xterm, mono will bail out when attempting to write to the console.
export TERM=xterm

# run installer
./internal/unix/install
