param (
    [string]$directoryPath,
    [string]$outputJsonPath
)

# Create an empty array to hold the results
$results = @()

# Get all files in the directory
$files = Get-ChildItem -Path $directoryPath

# Loop through each file
foreach ($file in $files) {
    # Run Convert-PsoImageToText on each file
    $textObject = Convert-PsoImageToText -Path $file.FullName -Language ru-RU

    # Initialize an empty text string
    $text = ""

    # Check if the text object's Text property is an array or a single string
    if ($textObject.Text -is [System.Array]) {
        # If it's an array, join the array elements into a single string
        $text = $textObject.Text -join " "
    } else {
        # If it's a single string, use it directly
        $text = $textObject.Text
    }

    # Create a custom object with file and text
    $result = @{
        file = $file.Name
        text = $text
    }

    # Add the result to the results array
    $results += $result
}

# Convert the results to JSON and save to the output file
$results | ConvertTo-Json | Out-File -FilePath $outputJsonPath -Encoding UTF8

# Output completion message
Write-Host "Text extraction and JSON export complete. File saved to: $outputJsonPath"
