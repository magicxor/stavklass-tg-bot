# Required only for PowerShell 7+
# [System.Reflection.Assembly]::LoadWithPartialName("System.Drawing")

param (
    [string]$jsonFilePath,
    [string]$photosDirectoryPath
)

# Ensure UTF-8 encoding when reading the JSON file
$jsonContent = Get-Content -Path $jsonFilePath -Raw -Encoding UTF8 | ConvertFrom-Json

# Iterate through each item in the JSON data
foreach ($item in $jsonContent) {
    # Define the full path to the photo
    $photoPath = Join-Path -Path $photosDirectoryPath -ChildPath $item.file

    # Use System.Drawing.Image to get the photo dimensions
    $image = [System.Drawing.Image]::FromFile($photoPath)
    $width = $image.Width
    $height = $image.Height
    $image.Dispose()

    # Add the width and height to the JSON object
    $item | Add-Member -MemberType NoteProperty -Name "width" -Value $width
    $item | Add-Member -MemberType NoteProperty -Name "height" -Value $height
}

# Convert the updated JSON data back to a JSON string with UTF-8 encoding
$updatedJson = $jsonContent | ConvertTo-Json -Depth 100 -Compress

# Explicitly specify UTF-8 encoding when writing the updated JSON data back to the file
$updatedJson | Out-File -FilePath $jsonFilePath -Encoding UTF8

# Output completion message
Write-Host "JSON file updated with photo dimensions."
