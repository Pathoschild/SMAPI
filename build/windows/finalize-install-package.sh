#!/usr/bin/env bash

##########
## Read config
##########
# get SMAPI version
version="$1"
if [ $# -eq 0 ]; then
    echo "SMAPI release version (like '4.0.0'):"
    read version
fi

# get Windows bin path
windowsBinPath="$2"
if [ $# -le 1 ]; then
    echo "Windows compiled bin path:"
    read windowsBinPath
fi

# installer internal folders
buildFolders=("linux" "macOS" "windows")


##########
## Finalize release package
##########
for folderName in "SMAPI $version installer" "SMAPI $version installer for developers"; do
    # move files to Linux filesystem
    echo "Preparing $folderName.zip..."
    echo "-------------------------------------------------"
    echo "copying '$windowsBinPath/$folderName' to Linux filesystem..."
    cp -r "$windowsBinPath/$folderName" .

    # fix permissions
    echo "fixing permissions..."
    find "$folderName" -type d -exec chmod 755 {} \;
    find "$folderName" -type f -exec chmod 644 {} \;
    find "$folderName" -name "*.sh" -exec chmod 755 {} \;
    find "$folderName" -name "*.command" -exec chmod 755 {} \;
    find "$folderName" -name "SMAPI.Installer" -exec chmod 755 {} \;
    find "$folderName" -name "StardewModdingAPI" -exec chmod 755 {} \;

    # convert bundle folder into final 'install.dat' files
    for build in ${buildFolders[@]}; do
        echo "packaging $folderName/internal/$build/install.dat..."
        pushd "$folderName/internal/$build/bundle" > /dev/null
        zip "install.dat" * --recurse-paths --quiet
        mv install.dat ../
        popd > /dev/null

        rm -rf "$folderName/internal/$build/bundle"
    done

    # zip installer
    echo "packaging installer..."
    zip -9 "$folderName.zip" "$folderName" --recurse-paths --quiet

    # move zip back to Windows bin path
    echo "moving release zip to $windowsBinPath/$folderName.zip..."
    mv "$folderName.zip" "$windowsBinPath"
    rm -rf "$folderName"

    echo ""
    echo ""
done

echo "Done!"
