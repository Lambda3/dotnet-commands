$ErrorActionPreference = "Stop"
Push-Location $PSScriptRoot\bin\Debug\netcoreapp1.0\publish
if (gcm tar -ErrorAction Ignore) {
    $gzip = $([System.IO.Path]::GetFullPath("$(pwd)\..\dotnet-commands.tar.gz"))
    echo "Creating '$gzip'..."
    if (Test-Path $gzip) { rm $gzip }
    tar -cvzf ../dotnet-commands.tar.gz . > $null
    if ($LASTEXITCODE -ne 0) {
        echo "Error publishing."
        Pop-Location
        exit 1
    }
}
Add-Type -assembly System.IO.Compression.FileSystem
$zip = $([System.IO.Path]::GetFullPath("$(pwd)\..\dotnet-commands.zip"))
echo "Creating '$zip'..."
if (Test-Path $zip) { rm $zip }
[System.IO.Compression.ZipFile]::CreateFromDirectory($(pwd), "$(pwd)\..\dotnet-commands.zip")
Pop-Location