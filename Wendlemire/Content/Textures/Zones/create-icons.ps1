# PowerShell script to create circular icon versions of biome images
# Resizes to 256x256 and crops into circles with transparent background

Add-Type -AssemblyName System.Drawing

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$sourceDir = $scriptDir
$outputDir = Join-Path $scriptDir "Icons"

# Create output directory if it doesn't exist
if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir | Out-Null
    Write-Host "Created Icons directory: $outputDir" -ForegroundColor Green
}

# Get all PNG images in the source directory (excluding subdirectories)
$images = Get-ChildItem -Path $sourceDir -Filter "*.png" -File

Write-Host "Found $($images.Count) images to process" -ForegroundColor Cyan

foreach ($image in $images) {
    Write-Host "Processing: $($image.Name)" -ForegroundColor Yellow
    
    try {
        # Load the source image
        $srcImage = [System.Drawing.Image]::FromFile($image.FullName)
        
        # Create a new 256x256 bitmap with transparency support
        $size = 256
        $destBitmap = New-Object System.Drawing.Bitmap($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        
        # Create graphics object for drawing
        $graphics = [System.Drawing.Graphics]::FromImage($destBitmap)
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        
        # Clear with transparent background
        $graphics.Clear([System.Drawing.Color]::Transparent)
        
        # Create circular clipping path
        $path = New-Object System.Drawing.Drawing2D.GraphicsPath
        $path.AddEllipse(0, 0, $size, $size)
        $graphics.SetClip($path)
        
        # Calculate source rectangle for center crop (square from center of original)
        $srcWidth = $srcImage.Width
        $srcHeight = $srcImage.Height
        $cropSize = [Math]::Min($srcWidth, $srcHeight)
        $srcX = ($srcWidth - $cropSize) / 2
        $srcY = ($srcHeight - $cropSize) / 2
        
        $srcRect = New-Object System.Drawing.Rectangle($srcX, $srcY, $cropSize, $cropSize)
        $destRect = New-Object System.Drawing.Rectangle(0, 0, $size, $size)
        
        # Draw the resized image
        $graphics.DrawImage($srcImage, $destRect, $srcRect, [System.Drawing.GraphicsUnit]::Pixel)
        
        # Save the circular icon
        $outputPath = Join-Path $outputDir $image.Name
        $destBitmap.Save($outputPath, [System.Drawing.Imaging.ImageFormat]::Png)
        
        Write-Host "  -> Saved: $outputPath" -ForegroundColor Green
        
        # Cleanup
        $graphics.Dispose()
        $path.Dispose()
        $destBitmap.Dispose()
        $srcImage.Dispose()
    }
    catch {
        Write-Host "  Error processing $($image.Name): $_" -ForegroundColor Red
    }
}

Write-Host "`nDone! Created $($images.Count) circular icons in $outputDir" -ForegroundColor Cyan
