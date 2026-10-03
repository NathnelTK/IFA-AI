# =============================================================================
# setup-ollama.ps1 — prepare a local, free Ollama model for the IFA backend.
#
# Ollama is the zero-cost LLM provider in src/IFA.Infrastructure/AI. When it is
# reachable the LlmGateway uses it; when it is not, the gateway silently falls
# back to the deterministic FallbackLlmProvider (so course creation still works,
# just with templated content).
#
# This script:
#   1. verifies the `ollama` CLI is installed (prints install instructions if not)
#   2. makes sure the server is listening on OLLAMA_BASE_URL (default :11434)
#   3. pulls the model the backend is configured to use
#   4. smoke-tests a generation call
#
# Usage:
#   powershell -ExecutionPolicy Bypass -File scripts/setup-ollama.ps1
#   powershell -ExecutionPolicy Bypass -File scripts/setup-ollama.ps1 -Model llama3.2:3b
# =============================================================================

[CmdletBinding()]
param(
    # Default matches src/IFA.API/appsettings.json -> Ollama:Model
    [string]$Model = 'llama3.2:3b',
    [string]$BaseUrl = 'http://localhost:11434'
)

$ErrorActionPreference = 'Stop'

function Write-Step($message) { Write-Host "==> $message" -ForegroundColor Cyan }
function Write-Ok($message)   { Write-Host "    $message" -ForegroundColor Green }
function Write-Warn2($message){ Write-Host "    $message" -ForegroundColor Yellow }

Write-Step 'Checking for the Ollama CLI'
$ollama = Get-Command ollama -ErrorAction SilentlyContinue
if (-not $ollama) {
    Write-Warn2 'The `ollama` CLI was not found on PATH.'
    Write-Host @"
    Install Ollama first (one command, no Docker required):
      Windows : winget install Ollama.Ollama
                (or download from https://ollama.com/download)
      macOS   : brew install ollama
      Linux   : curl -fsSL https://ollama.com/install.sh | sh

    After installing, open a new terminal so PATH is refreshed, then re-run this script.
"@
    exit 1
}
Write-Ok "Found: $($ollama.Source)"

# --- Ensure the server is up -------------------------------------------------
Write-Step "Ensuring the Ollama server is listening on $BaseUrl"
$port = ([Uri]$BaseUrl).Port
$listening = Test-NetConnection -ComputerName 'localhost' -Port $port -WarningAction SilentlyContinue
if (-not $listening.TcpTestSucceeded) {
    Write-Warn2 'Server not detected. Starting `ollama serve` in a new window...'
    Start-Process -FilePath 'ollama' -ArgumentList 'serve' -WindowStyle Minimized | Out-Null
    for ($i = 0; $i -lt 20; $i++) {
        Start-Sleep -Milliseconds 750
        if ((Test-NetConnection -ComputerName 'localhost' -Port $port -WarningAction SilentlyContinue).TcpTestSucceeded) { break }
    }
}
if (-not (Test-NetConnection -ComputerName 'localhost' -Port $port -WarningAction SilentlyContinue).TcpTestSucceeded) {
    Write-Warn2 "Still not reachable on $BaseUrl. Start it manually with: ollama serve"
    exit 1
}
Write-Ok 'Server is reachable.'

# --- Pull the model ----------------------------------------------------------
Write-Step "Pulling model '$Model' (first run downloads a few GB)"
& ollama pull $Model
if ($LASTEXITCODE -ne 0) { Write-Warn2 "ollama pull '$Model' failed."; exit 1 }
Write-Ok 'Model available.'

# --- Smoke test --------------------------------------------------------------
Write-Step 'Smoke-testing the model'
try {
    $body = @{
        model  = $Model
        prompt = 'Reply with the single JSON object {"status":"ok"}.'
        stream = $false
        format = 'json'
    } | ConvertTo-Json
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $resp = Invoke-RestMethod "$($BaseUrl.TrimEnd('/'))/api/generate" -Method Post -ContentType 'application/json' -Body $body -TimeoutSec 180
    $sw.Stop()
    Write-Ok "Model responded in $([int]$sw.Elapsed.TotalSeconds)s."
} catch {
    Write-Warn2 "Generation test failed: $($_.Exception.Message)"
    exit 1
}

Write-Host ''
Write-Host 'Ollama is ready. Point the backend at it:' -ForegroundColor Green
Write-Host "    AI_DEFAULT_PROVIDER=Ollama   (or per-role Ai:Pipelines:*:Provider)"
Write-Host "    OLLAMA_BASE_URL=$BaseUrl"
Write-Host "    Ollama:Model=$Model"
Write-Host ''
Write-Host 'Restart the API afterwards so the gateway picks up the provider.'
