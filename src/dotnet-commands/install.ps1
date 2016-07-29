$ErrorActionPreference = "Stop"
$uri="https://github.com/Lambda3/dotnet-commands/releases/download/0.0.1-alpha1-build6/dotnet-commands.zip"
$outFile=[System.IO.Path]::GetTempFileName()
Invoke-WebRequest -uri $uri -OutFile $outFile
Add-Type -assembly System.IO.Compression.FileSystem
$outDir = "$outFile-extracted"
[System.IO.Compression.ZipFile]::ExtractToDirectory($outFile, $outDir)
Write-Host $outDir
. "$outDir\dotnet-commands.cmd" install dotnet-commands --verbose --pre
if ($LASTEXITCODE -ne 0) {
    Write-Host "Could not install."
    exit 1
}
$path = [Environment]::GetEnvironmentVariable("PATH")
#Write-Host "Path is $path"
$newPath = "$env:USERPROFILE\.nuget\commands\bin;$path"
#Write-Host "NewPath is $newPath"
[Environment]::SetEnvironmentVariable("PATH", $newPath, "User")