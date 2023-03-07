#!/usr/bin/env bash

cd "`dirname "$0"`"

xattr -r -d com.apple.quarantine internal
internal/macOS/SMAPI.Installer
