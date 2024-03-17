param (
    [string]$jsonFilePath,
    [string]$photosDirectory
)

# Ensure the photos directory path is absolute
$photosDirectory = Resolve-Path $photosDirectory

# Read the JSON content and convert to an object
$jsonContent = Get-Content -Path $jsonFilePath -Raw -Encoding UTF8 | ConvertFrom-Json

# Initialize an array to hold the items to keep
$itemsToKeep = @()

foreach ($item in $jsonContent) {
    $filePath = Join-Path -Path $photosDirectory -ChildPath $item.file
    if (Test-Path -Path $filePath) {
        # If the file exists, add the item to the list to keep
        $itemsToKeep += $item
    } else {
        Write-Host "File $($item.file) not found. Removing entry from catalog."
    }
}

# Convert the updated list back to JSON
$updatedJsonContent = $itemsToKeep | ConvertTo-Json -Depth 10

# Save the updated JSON content back to the file
$updatedJsonContent | Out-File -FilePath $jsonFilePath -Encoding UTF8

Write-Host "Catalog updated successfully."
