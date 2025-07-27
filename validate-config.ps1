# PowerShell script to validate _config.yml syntax
Write-Host "Validating _config.yml syntax..." -ForegroundColor Green

try {
    # Read the YAML file
    $lines = Get-Content "_config.yml"
    $lineNumber = 0
    $errors = @()
    
    foreach ($line in $lines) {
        $lineNumber++
        
        # Skip comments and empty lines
        if ($line -match '^\s*#' -or $line -match '^\s*$') {
            continue
        }
        
        # Check for tabs (YAML should use spaces)
        if ($line -match '\t') {
            $errors += "Line $lineNumber`: Contains tabs (use spaces instead)"
        }
        
        # Check for problematic empty values (like "key: " with trailing space but no value)
        # This is more specific - only flag obvious empty values, not YAML structures
        if ($line -match '^\s*\w+:\s+$') {
            $errors += "Line $lineNumber`: Empty value with trailing space - consider removing space or adding value"
        }
    }
    
    # Additional check: Look for the specific pattern that caused the original error
    $content = Get-Content "_config.yml" -Raw
    if ($content -match 'username:\s*\n' -or $content -match 'app_id:\s*\n' -or $content -match 'publisher:\s*\n') {
        $errors += "Found empty social media configuration values - these should be commented out or have values"
    }
    
    if ($errors.Count -eq 0) {
        Write-Host "✅ _config.yml syntax appears valid!" -ForegroundColor Green
        Write-Host "Ready to test locally or deploy to GitHub Pages." -ForegroundColor Cyan
    } else {
        Write-Host "❌ Found potential YAML issues:" -ForegroundColor Red
        foreach ($error in $errors) {
            Write-Host "  $error" -ForegroundColor Yellow
        }
    }
    
} catch {
    Write-Host "❌ Error reading _config.yml: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n📋 Testing Options:" -ForegroundColor Cyan
Write-Host "  1. Quick Docker test: docker-compose -f docker-compose.jekyll.yml up" -ForegroundColor White
Write-Host "  2. Ruby/Jekyll:       test-jekyll.bat" -ForegroundColor White
Write-Host "  3. Just build check:  docker run --rm -v ${PWD}:/srv/jekyll jekyll/jekyll:4.3.0 jekyll build" -ForegroundColor White
Write-Host "`n🌐 After testing locally, your site will be at: http://localhost:4000" -ForegroundColor Green