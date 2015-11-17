param(
    [string]$inputDir = $(throw "Specify the full path to the directory which needs to be harvested")
)

. "$PSScriptRoot\..\..\scripts\_common.ps1"

$DotnetMSIOutput = ""
$WixRoot = ""
$InstallFileswsx = "install-files.wxs"
$InstallFilesWixobj = "install-files.wixobj"

function AcquireWixTools
{
    return "C:\Program Files (x86)\WiX Toolset v3.10\bin"
}

function RunHeat
{
    $result = $true    
    pushd "$WixRoot"    

    Write-Host Running heat..

    .\heat.exe dir `"$inputDir`" -template fragment -sreg -gg -var var.DotnetSrc -cg InstallFiles -srd -dr DOTNETHOME -out $InstallFileswsx   

    if($LastExitCode -ne 0)
    {
        $result = $false
        Write-Host "Heat failed with exit code $LastExitCode."
    }

    popd
    return $result
}

function RunCandle
{
    $result = $true
    pushd "$WixRoot"

    Write-Host Running candle..

    .\candle.exe -dDotnetSrc="$inputDir" -dMicrosoftEula="$RepoRoot\packaging\osx\resources\en.lproj\eula.rtf"  "$RepoRoot\packaging\windows\dotnet.wxs" $InstallFileswsx

    if($LastExitCode -ne 0)
    {
        $result = $false
        Write-Host "Candle failed with exit code $LastExitCode."
    }

    popd
    return $result
}

function RunLight
{
    $result = $true
    pushd "$WixRoot"

    Write-Host Running light..

    .\light -ext WixUIExtension -cultures:en-us  dotnet.wixobj $InstallFilesWixobj -out $DotnetMSIOutput

    if($LastExitCode -ne 0)
    {
        $result = $false
        Write-Host "Light failed with exit code $LastExitCode."
    }

    popd
    return $result
}

if(!(Test-Path $inputDir))
{
    throw "$inputDir not found"
}

if(!(Test-Path $PackageDir)) 
{
    mkdir $PackageDir | Out-Null
}

$DotnetMSIOutput = Join-Path $PackageDir "dotnet-win-x64.$DOTNET_BUILD_VERSION.msi"

Write-Host "Creating dotnet MSI at $DotnetMSIOutput"

$WixRoot = AcquireWixTools

if([string]::IsNullOrEmpty($WixRoot))
{
    return -1
}

if(-Not (RunHeat))
{
    return -1
}

if(-Not (RunCandle))
{
    return -1
}

if(-Not (RunLight))
{
    return -1
}

if(!(Test-Path $DotnetMSIOutput))
{
    throw "Unable to create the dotnet msi."
}

Write-Host -ForegroundColor Green "Successfully create dotnet MSI - $DotnetMSIOutput"

return 0
