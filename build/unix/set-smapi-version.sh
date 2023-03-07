#!/usr/bin/env bash

#
#
# This is the Bash equivalent of ../windows/set-smapi-version.ps1.
# When making changes, both scripts should be updated.
#
#


# get version number
version="$1"
if [ $# -eq 0 ]; then
    echo "SMAPI release version (like '4.0.0'):"
    read version
fi

# move to SMAPI root
cd "`dirname "$0"`/../.."

# apply changes
sed "s/<Version>.+<\/Version>/<Version>$version<\/Version>/" "build/common.targets" --in-place --regexp-extended
sed "s/RawApiVersion = \".+?\";/RawApiVersion = \"$version\";/" "src/SMAPI/Constants.cs" --in-place --regexp-extended
for modName in "ConsoleCommands" "ErrorHandler" "SaveBackup"; do
    sed "s/\"(Version|MinimumApiVersion)\": \".+?\"/\"\1\": \"$version\"/g" "src/SMAPI.Mods.$modName/manifest.json" --in-place --regexp-extended
done
