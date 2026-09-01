# Shader Compilation Script for Wendlemire
# Compiles .fx shader files to MonoGame effect format (.mgfxo)
# Usage: .\compile-shaders.ps1

$ErrorActionPreference = "Stop"

# Configuration
$ProjectRoot = $PSScriptRoot
$ShaderSourceDir = Join-Path $ProjectRoot "Wendlemire\Content\Effects"
$ShaderOutputDir = Join-Path $ProjectRoot "Wendlemire\Content\Effects\Compiled"
$Platform = "DesktopGL"

# Colors for output
function Write-Success { param($msg) Write-Host $msg -ForegroundColor Green }
function Write-Info { param($msg) Write-Host $msg -ForegroundColor Cyan }
function Write-Warn { param($msg) Write-Host $msg -ForegroundColor Yellow }

Write-Host ""
Write-Host "========================================" -ForegroundColor Magenta
Write-Host "  Wendlemire Shader Compiler" -ForegroundColor Magenta
Write-Host "========================================" -ForegroundColor Magenta
Write-Host ""

# Check if mgfxc is installed
Write-Info "Checking for MonoGame Effect Compiler (mgfxc)..."

$mgfxcInstalled = $null
try {
    $mgfxcInstalled = & dotnet tool list -g 2>&1 | Select-String "dotnet-mgfxc"
} catch {
    # Ignore errors
}

if (-not $mgfxcInstalled) {
    Write-Warn "mgfxc not found. Installing MonoGame Effect Compiler..."
    & dotnet tool install -g dotnet-mgfxc
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Failed to install mgfxc. Please install manually:" -ForegroundColor Red
        Write-Host "  dotnet tool install -g dotnet-mgfxc" -ForegroundColor Yellow
        exit 1
    }
    Write-Success "mgfxc installed successfully!"
} else {
    Write-Success "mgfxc is already installed."
}

# Create output directory if it doesn't exist
if (-not (Test-Path $ShaderOutputDir)) {
    New-Item -ItemType Directory -Path $ShaderOutputDir -Force | Out-Null
    Write-Info "Created output directory: $ShaderOutputDir"
}

# Find all .fx files
$shaderFiles = Get-ChildItem -Path $ShaderSourceDir -Filter "*.fx" -File -ErrorAction SilentlyContinue

if ($shaderFiles.Count -eq 0) {
    Write-Warn "No .fx shader files found in $ShaderSourceDir"
    exit 0
}

Write-Info "Found $($shaderFiles.Count) shader file(s) to compile..."
Write-Host ""

$successCount = 0
$failCount = 0

foreach ($shader in $shaderFiles) {
    $inputPath = $shader.FullName
    $outputName = [System.IO.Path]::GetFileNameWithoutExtension($shader.Name) + ".mgfxo"
    $outputPath = Join-Path $ShaderOutputDir $outputName
    
    Write-Host "  Compiling: " -NoNewline
    Write-Host $shader.Name -ForegroundColor White -NoNewline
    Write-Host " -> " -NoNewline
    Write-Host $outputName -ForegroundColor White -NoNewline
    Write-Host "... " -NoNewline
    
    try {
        # Run mgfxc to compile the shader
        # /Profile:OpenGL for DesktopGL platform
        $result = & mgfxc $inputPath $outputPath /Profile:OpenGL 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Write-Success "OK"
            $successCount++
        } else {
            Write-Host "FAILED" -ForegroundColor Red
            Write-Host "    Error: $result" -ForegroundColor Red
            $failCount++
        }
    } catch {
        Write-Host "FAILED" -ForegroundColor Red
        Write-Host "    Exception: $_" -ForegroundColor Red
        $failCount++
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Magenta
Write-Host "  Compilation Complete" -ForegroundColor Magenta
Write-Host "========================================" -ForegroundColor Magenta
Write-Host ""
Write-Host "  Success: $successCount" -ForegroundColor Green
if ($failCount -gt 0) {
    Write-Host "  Failed:  $failCount" -ForegroundColor Red
}
Write-Host ""

if ($failCount -gt 0) {
    exit 1
}

Write-Success "All shaders compiled successfully!"
Write-Info "Compiled shaders are in: $ShaderOutputDir"
Write-Host ""



