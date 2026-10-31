param(
    [Parameter(Mandatory=$true)][string]$InstallerPath,
    [Parameter(Mandatory=$true)][string[]]$AllowedSigners
)

# Locate signtool – usual location on Windows runners
$signTool = "$env:ProgramFiles\Windows Kits\10\bin\x64\signtool.exe"
if (-not (Test-Path $signTool)) {
    throw "signtool.exe not found at $signTool"
}

# Verify signature and capture output
$info = & $signTool verify /pa /v "$InstallerPath" 2>&1
if ($LASTEXITCODE -ne 0) {
    throw "Failed to verify installer signature: $info"
}

# Extract Subject line
if ($info -match "Subject:\s*(.+)$") {
    $subject = $matches[1].Trim()
} else {
    throw "Unable to extract Subject from signtool output."
}

if (-not ($AllowedSigners -contains $subject)) {
    throw "Unexpected installer signer: $subject"
}

Write-Host "Installer signature validated successfully: $subject"
