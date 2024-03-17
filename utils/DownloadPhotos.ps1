param (
    [string]$csvPath,
    [string]$downloadPath,
    [string]$jsonFilePath,
    [string]$startNumber
)

# Load the ImageSharp assembly - ensure you have the correct path
Add-Type -Path (Join-Path $PSScriptRoot "lib" "SixLabors.ImageSharp.3.1.3" "lib" "net6.0" "SixLabors.ImageSharp.dll")

# Ensure the download directory exists
if (-not (Test-Path -Path $downloadPath)) {
    New-Item -ItemType Directory -Force -Path $downloadPath
}
# Initialize the list to hold file info
$fileInfoList = @()

# Define request headers
$headers = @{
    "User-Agent" = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:123.0) Gecko/20100101 Firefox/123.0"
    "Accept-Language" = "en,en-US;q=0.5"
    "Accept-Encoding" = "gzip, deflate, br"
    "Upgrade-Insecure-Requests" = "1"
    "Sec-Fetch-Dest" = "document"
    "Sec-Fetch-Mode" = "navigate"
    "Sec-Fetch-Site" = "cross-site"
    "Pragma" = "no-cache"
    "Cache-Control" = "no-cache"
}

# Read the CSV lines
$lines = Get-Content $csvPath -Encoding UTF8
$counter = [int]$startNumber

foreach ($line in $lines) {
    $download_error = $false

    # Split line into URL and description
    $parts = $line -split ';',2
    $url = $parts[0]
    $description = $parts[1]

    # Extract file extension
    $fileExtension = [System.IO.Path]::GetExtension($url)

    # Validate and default to .jpg if extension is missing or not an image extension
    if (-not $fileExtension -or $fileExtension -notin @('.jpg', '.jpeg', '.png', '.gif', '.bmp', '.webp', '.avif')) {
        $fileExtension = '.jpg'
    }

    # Define the file name with original extension
    $fileName = "$counter$fileExtension"
    $filePath = Join-Path $downloadPath $fileName

    try {
        # Download the file
        Invoke-WebRequest -Uri $url -OutFile $filePath -Headers $headers -ErrorAction Stop

        # Use ImageSharp to get the photo dimensions
        $image = [SixLabors.ImageSharp.Image]::Load($filePath)
        $width = $image.Width
        $height = $image.Height
        $image.Dispose()

        # Add file info to list
        $fileInfo = @{
            text = $description
            file = $fileName
            width = $width
            height = $height
        }
        $fileInfoList += $fileInfo
    }
    catch {
        $download_error = $true

        # Write error to console
        Write-Host "Error downloading `'$url`': $_"
    }
    finally {
        if ($download_error) {
            # Ignore
        }
        else {
            Write-Host "Downloaded file: $url"
        }
    }

    # Increment counter for next file name
    $counter++
}

# Convert the list to JSON
$jsonContent = $fileInfoList | ConvertTo-Json -Depth 10

# Use Out-File with explicit encoding for more control
Out-File -FilePath $jsonFilePath -Encoding UTF8 -InputObject $jsonContent

Write-Host "Download completed. JSON file created at: $jsonFilePath"
