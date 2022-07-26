#!/bin/bash

cd "$(dirname "$0")" || exit

xattr -r -d com.apple.quarantine internal
internal/macOS/SMAPI.Installer
