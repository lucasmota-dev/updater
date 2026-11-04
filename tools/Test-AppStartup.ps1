<#
.SYNOPSIS
    Confirma que o executável abre e continua em execução (teste de fumaça).
.DESCRIPTION
    Inicia o executável, espera alguns segundos e falha se o processo já tiver terminado.
    Pega erros que só aparecem na execução, como XAML inválido ou chave de recurso duplicada,
    que o compilador e os testes unitários não detectam.
#>
param(
    [Parameter(Mandatory = $true)][string]$ExePath,
    [int]$Seconds = 15
)

$ErrorActionPreference = 'Stop'

$resolved = (Resolve-Path -LiteralPath $ExePath).Path
$startedAt = Get-Date

# O executor do GitHub Actions roda como administrador. Sem esta variável, o app se reabriria sem
# elevação (veja ElevationGuard) e este teste veria o processo original encerrar.
$env:APP_UPDATER_ALLOW_ELEVATED = '1'
$process = Start-Process -FilePath $resolved -PassThru

try {
    if ($process.WaitForExit($Seconds * 1000)) {
        Write-Host "::error::O aplicativo encerrou durante a inicialização (código $($process.ExitCode))."
        Get-WinEvent -FilterHashtable @{ LogName = 'Application'; ProviderName = '.NET Runtime'; StartTime = $startedAt } -MaxEvents 1 -ErrorAction SilentlyContinue |
            ForEach-Object { Write-Host $_.Message }
        exit 1
    }

    Write-Host "O aplicativo continuou em execução por $Seconds segundos."
}
finally {
    if (-not $process.HasExited) {
        Stop-Process -Id $process.Id -Force
    }
}
