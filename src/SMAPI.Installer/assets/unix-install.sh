#!/bin/bash

# Move to script's directory
cd "`dirname "$0"`"

# make sure .NET 5 is installed
if ! command -v dotnet >/dev/null 2>&1; then
    echo "Oops! You must have .NET 5 installed to use SMAPI: https://dotnet.microsoft.com/download";
    read
    exit 1
fi

# run installer
dotnet internal/unix/SMAPI.Installer.dll
