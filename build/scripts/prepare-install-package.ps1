#!/usr/bin/env pwsh

#
#
# Note: On Windows, this script *does not* set Linux permissions. The final changes are handled by the
# finalize-install-package.sh file in WSL.
#
#


##########
## Validate environment
##########
if ($PSVersionTable.PSVersion.Major -lt 7) {
    Write-Error "This script needs PowerShell 7 or later. Run it using 'pwsh' instead." # needed for `$IsWindows`
    exit 1
}


##########
## Read arguments
##########
$windowsOnly = $false # Windows-only build
$skipBundleDeletion = $false # skip bundle deletion (only applies when using WSL to finalize the build on Windows)
foreach ($arg in $args) {
    if ($arg -eq "--windows-only" -and $IsWindows) {
        $windowsOnly = $true
    }
    elseif ($arg -eq "--skip-bundle-deletion") {
        $skipBundleDeletion = $true
    }
}


##########
## Find the game folder
##########
if ($IsWindows) {
    $possibleGamePaths=(
        # GOG
        "C:\Program Files\GalaxyClient\Games\Stardew Valley",
        "C:\Program Files\GOG Galaxy\Games\Stardew Valley",
        "C:\Program Files\GOG Games\Stardew Valley",
        "C:\Program Files (x86)\GalaxyClient\Games\Stardew Valley",
        "C:\Program Files (x86)\GOG Galaxy\Games\Stardew Valley",
        "C:\Program Files (x86)\GOG Games\Stardew Valley",

        # Steam
        "C:\Program Files\Steam\steamapps\common\Stardew Valley",
        "C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley"
    )
}
else {
    $possibleGamePaths=(
        # override
        "$HOME/StardewValley",

        # Linux
        "$HOME/GOG Games/Stardew Valley/game",
        "$HOME/.steam/steam/steamapps/common/Stardew Valley",
        "$HOME/.local/share/Steam/steamapps/common/Stardew Valley",
        "$HOME/.var/app/com.valvesoftware.Steam/data/Steam/steamapps/common/Stardew Valley",

        # macOS
        "/Applications/Stardew Valley.app/Contents/MacOS",
        "$HOME/Library/Application Support/Steam/steamapps/common/Stardew Valley/Contents/MacOS"
    )
}

$gamePath = ""
foreach ($possibleGamePath in $possibleGamePaths) {
    if (Test-Path $possibleGamePath -PathType Container) {
        $gamePath = $possibleGamePath
        break
    }
}


##########
## Preset values
##########
# paths
$bundleModNames = "ConsoleCommands", "SaveBackup"

# build configuration
$buildConfig = "Release"
$framework = "net6.0"
if ($windowsOnly) {
    $folders = "windows"
    $runtimes = @{ windows = "win-x64" }
    $msBuildPlatformNames = @{ windows = "Windows_NT" }
}
else {
    $folders = "linux", "macOS", "windows"
    $runtimes = @{ linux = "linux-x64"; macOS = "osx-x64"; windows = "win-x64" }
    $msBuildPlatformNames = @{ linux = "Unix"; macOS = "OSX"; windows = "Windows_NT" }
}

# version number
$version = $args[0]
if (!$version) {
    $version = Read-Host "SMAPI release version (like '4.0.0')"
}


##########
## Move to SMAPI root
##########
Set-Location "$PSScriptRoot/../.."


##########
## Clear old build files
##########
Write-Output "Clearing old builds..."
Write-Output "-------------------------------------------------"

foreach ($path in (Get-ChildItem -Recurse -Include ('bin', 'obj'))) {
    Write-Output "$path"
    Remove-Item -Recurse -Force "$path"
}
Write-Output ""


##########
## Compile files
##########
. "$PSScriptRoot/set-smapi-version.ps1" "$version"
foreach ($folder in $folders) {
    $runtime = $runtimes[$folder]
    $msbuildPlatformName = $msBuildPlatformNames[$folder]

    Write-Output "Compiling SMAPI for $folder..."
    Write-Output "-------------------------------------------------"
    dotnet publish src/SMAPI --configuration $buildConfig -v minimal --runtime "$runtime" --framework "$framework" -p:OS="$msbuildPlatformName" -p:TargetFrameworks="$framework" -p:GamePath="$gamePath" -p:CopyToGameFolder="false" --self-contained true
    Write-Output ""
    Write-Output ""

    Write-Output "Compiling installer for $folder..."
    Write-Output "-------------------------------------------------"
    dotnet publish src/SMAPI.Installer --configuration $buildConfig -v minimal --runtime "$runtime" --framework "$framework" -p:OS="$msbuildPlatformName" -p:TargetFrameworks="$framework" -p:GamePath="$gamePath" -p:CopyToGameFolder="false" --self-contained true
    Write-Output ""
    Write-Output ""

    foreach ($modName in $bundleModNames) {
        Write-Output "Compiling $modName for $folder..."
        Write-Output "-------------------------------------------------"
        dotnet publish src/SMAPI.Mods.$modName --configuration $buildConfig -v minimal --runtime "$runtime" --framework "$framework" -p:OS="$msbuildPlatformName" -p:TargetFrameworks="$framework" -p:GamePath="$gamePath" -p:CopyToGameFolder="false" --self-contained false
        Write-Output ""
        Write-Output ""
    }
}


##########
## Prepare install package
##########
Write-Output "Preparing install package..."
Write-Output "----------------------------"

# init paths
$installAssets = "src/SMAPI.Installer/assets"
$packagePath = "bin/SMAPI installer"

# init structure
Write-Host "Setting up structure..."
foreach ($folder in $folders) {
    $folderPath = "$packagePath/internal/$folder/bundle/smapi-internal"

    if ($IsWindows) {
        # On Windows, mkdir creates parent directories automatically and the --parents argument isn't recognized.
        mkdir "$folderPath" > $null
    }
    else
    {
        mkdir "$folderPath" --parents
    }
}

# copy base installer files
foreach ($name in @("install on Linux.sh", "install on macOS.command", "install on Windows.bat", "README.txt")) {
    if ($windowsOnly -and ($name -eq "install on Linux.sh" -or $name -eq "install on macOS.command")) {
        continue;
    }

    Copy-Item "$installAssets/$name" "$packagePath"
}

# fetch 'malicious mods' blacklist
# SMAPI fetches it from the same URL automatically, but this avoids known malicious mods being loaded on first launch.
Write-Host "Fetching mod blacklist..."
$blacklistUrl = "https://smapi.io/SMAPI.blacklist.json" # same as BlacklistUrl in SMAPI.config.json
$blacklistPath = [System.IO.Path]::GetTempFileName()
try {
    Invoke-WebRequest -Uri "$blacklistUrl" -OutFile "$blacklistPath" -TimeoutSec 30 -MaximumRetryCount 2 -RetryIntervalSec 5
    $blacklistData = Get-Content "$blacklistPath" -Raw | ConvertFrom-Json
    Write-Host "   Fetched blacklist with $($blacklistData.Blacklist.Count) mod entries and $($blacklistData.LooseFileBlacklist.Count) loose file entries."
}
catch {
    Write-Warning "Couldn't fetch the mod blacklist from $blacklistUrl, so it'll be omitted from the release. SMAPI will fetch the latest blacklist when launched, but it won't take effect until the next launch. Error: $($_.Exception.Message)"
    Remove-Item "$blacklistPath"
    $blacklistPath = $null
}

# copy per-platform files
foreach ($folder in $folders) {
    $runtime = $runtimes[$folder]

    # get paths
    $smapiBin = "src/SMAPI/bin/$buildConfig/$runtime/publish"
    $internalPath = "$packagePath/internal/$folder"
    $bundlePath = "$internalPath/bundle"

    # installer files
    Copy-Item "src/SMAPI.Installer/bin/$buildConfig/$runtime/publish/*" "$internalPath" -Recurse
    Remove-Item -Recurse -Force "$internalPath/assets"

    # runtime config for SMAPI
    # This is identical to the one generated by the build, except that the min runtime version is
    # set to 6.0.0 (instead of whatever version it was built with) and rollForward is set to latestMinor instead of
    # minor.
    Copy-Item "$installAssets/runtimeconfig.json" "$bundlePath/StardewModdingAPI.runtimeconfig.json"

    # installer DLL config
    if ($folder -eq "windows") {
        Copy-Item "$installAssets/windows-exe-config.xml" "$packagePath/internal/windows/install.exe.config"
    }

    # bundle root files
    foreach ($name in @("StardewModdingAPI", "StardewModdingAPI.dll", "StardewModdingAPI.xml", "steam_appid.txt")) {
        if ($name -eq "StardewModdingAPI" -and $folder -eq "windows") {
            $name = "$name.exe"
        }

        Copy-Item "$smapiBin/$name" "$bundlePath"
    }

    # bundle i18n
    Copy-Item -Recurse "$smapiBin/i18n" "$bundlePath/smapi-internal"

    # bundle smapi-internal
    foreach ($name in @("0Harmony.dll", "0Harmony.xml", "Markdig.dll", "Mono.Cecil.dll", "Mono.Cecil.Mdb.dll", "Mono.Cecil.Pdb.dll", "MonoMod.Common.dll", "Newtonsoft.Json.dll", "Pathoschild.Http.Client.dll", "Pintail.dll", "TMXTile.dll", "SMAPI.Toolkit.dll", "SMAPI.Toolkit.xml", "SMAPI.Toolkit.CoreInterfaces.dll", "SMAPI.Toolkit.CoreInterfaces.xml", "System.Net.Http.Formatting.dll")) {
        Copy-Item "$smapiBin/$name" "$bundlePath/smapi-internal"
    }

    if ($folder -eq "windows") {
        Copy-Item "$smapiBin/VdfConverter.dll" "$bundlePath/smapi-internal"
    }

    if ($blacklistPath) {
        Copy-Item "$blacklistPath" "$bundlePath/smapi-internal/blacklist.json"
    }
    Copy-Item "$smapiBin/SMAPI.config.json" "$bundlePath/smapi-internal/config.json"
    Copy-Item "$smapiBin/SMAPI.metadata.json" "$bundlePath/smapi-internal/metadata.json"
    if ($folder -eq "linux" -or $folder -eq "macOS") {
        Copy-Item "$installAssets/unix-launcher.sh" "$bundlePath"
    }
    else {
        Copy-Item "$installAssets/windows-exe-config.xml" "$bundlePath/StardewModdingAPI.exe.config"
    }

    # copy bundled mods
    foreach ($modName in $bundleModNames) {
        $fromPath = "src/SMAPI.Mods.$modName/bin/$buildConfig/$runtime/publish"
        $targetPath = "$bundlePath/Mods/$modName"

        if ($IsWindows) {
            # On Windows, mkdir creates parent directories automatically and the --parents argument isn't recognized.
            mkdir "$targetPath" > $null
        }
        else
        {
            mkdir "$targetPath" --parents
        }

        Copy-Item "$fromPath/$modName.dll" "$targetPath"
        Copy-Item "$fromPath/manifest.json" "$targetPath"
        if (Test-Path "$fromPath/i18n" -PathType Container) {
            Copy-Item -Recurse "$fromPath/i18n" "$targetPath"
        }
    }
}

# remove temporary files
if ($blacklistPath) {
    Remove-Item "$blacklistPath"
}

# mark scripts executable
Write-Host "Setting file permissions..."
if ($IsWindows) {
    Write-Warning "Can't set Unix file permissions on Windows. This may cause issues for Linux/macOS players."
}
else {
    ForEach ($path in @("install on Linux.sh", "install on macOS.command", "internal/linux/bundle/unix-launcher.sh", "internal/macOS/bundle/unix-launcher.sh")) {
        if (Test-Path "$packagePath/$path" -PathType Leaf) {
            chmod 755 "$packagePath/$path"
        }
        else {
            Write-Host "Couldn't set permissions for '$packagePath/$path': file does not exist."
        }
    }
}

# convert bundle folder into final 'install.dat' files
Write-Host "Tucking SMAPI bundle into install.dat..."
foreach ($folder in $folders) {
    $path = "$packagePath/internal/$folder"

    if ($IsWindows) {
        Compress-Archive -Path "$path/bundle/*" -CompressionLevel Optimal -DestinationPath "$path/install.dat"
    }
    else {
        # Compress-Archive doesn't keep Unix permissions, so use zip directly on Linux/macOS
        pushd "$path/bundle" > /dev/null
        zip "install.dat" * --recurse-paths --quiet
        popd > /dev/null
        mv "$path/bundle/install.dat" "$path/install.dat"
    }

    if (!$skipBundleDeletion) {
        Remove-Item -Recurse -Force "$path/bundle"
    }
}


###########
### Create release zips
###########
Write-Host "Creating release zip..."
Move-Item "$packagePath" "bin/SMAPI $version installer"

if ($IsWindows) {
    Compress-Archive -Path "bin/SMAPI $version installer" -DestinationPath "bin/SMAPI-$version-installer.zip" -CompressionLevel Optimal
}
else {
    # Compress-Archive doesn't keep Unix permissions, so use zip directly on Linux/macOS
    pushd bin > /dev/null
    zip -9 "SMAPI-$version-installer.zip" "SMAPI $version installer" --recurse-paths --quiet
    popd > /dev/null
}

Write-Output ""
Write-Output "Done! Package created in ${pwd.Path}/bin."
