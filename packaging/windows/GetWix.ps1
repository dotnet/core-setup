Param(
    [string]$WixVersion="3.10.2",
    [string]$OutputDir="."
)

$wixFilename = "wix." + $WixVersion + ".zip"

$uri = "https://dotnetcli.blob.core.windows.net/build/wix/" + $wixFilename
$outFile = $OutputDir + "/" + $wixFilename

if (!(Test-Path "$OutputDir"))
{
    mkdir "$OutputDir" | Out-Null
}

if(!(Test-Path "$outFile"))
{
    Write-Output "Downloading WixTools.."
    Write-Output $uri
    Invoke-WebRequest -Uri $uri -OutFile $outFile
}

if(!(Test-Path "$OutputDir/candle.exe"))
{
    Write-Output "Extracting WixTools.."
    Write-Output $outFile
    Expand-Archive $outFile -DestinationPath $OutputDir
}
