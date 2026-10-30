<#
.SYNOPSIS
Calcula a próxima versão de release no padrão yyyy.MM.dd.NN (NN reinicia a cada dia).

.PARAMETER Date
Data da release no formato yyyy.MM.dd. Padrão: hoje no horário de Brasília.

.PARAMETER ExistingTags
Tags já existentes (ex.: v2026.10.28.01). Padrão: tags do repositório local.
#>
[CmdletBinding()]
param(
    [string]$Date,
    [string[]]$ExistingTags
)

$ErrorActionPreference = 'Stop'

if (-not $Date) {
    try {
        $zone = [TimeZoneInfo]::FindSystemTimeZoneById('E. South America Standard Time')
    }
    catch {
        $zone = [TimeZoneInfo]::FindSystemTimeZoneById('America/Sao_Paulo')
    }
    $Date = [TimeZoneInfo]::ConvertTimeFromUtc([DateTime]::UtcNow, $zone).ToString('yyyy.MM.dd')
}

if ($Date -notmatch '^\d{4}\.\d{2}\.\d{2}$') {
    throw "Data inválida '$Date'. Use yyyy.MM.dd."
}

# Rejeita datas que casam com o padrão mas não existem (ex.: 2026.13.45).
$parsed = [datetime]::MinValue
if (-not [datetime]::TryParseExact($Date, 'yyyy.MM.dd', [Globalization.CultureInfo]::InvariantCulture, [Globalization.DateTimeStyles]::None, [ref]$parsed)) {
    throw "Data inválida '$Date'. Use uma data real no formato yyyy.MM.dd."
}

if ($null -eq $ExistingTags) {
    $ExistingTags = @(git tag --list "v$Date.*")
}

$pattern = '^v?' + [regex]::Escape($Date) + '\.(\d+)$'
$last = 0
foreach ($tag in $ExistingTags) {
    if ($tag -match $pattern) {
        $last = [Math]::Max($last, [int]$Matches[1])
    }
}

'{0}.{1:00}' -f $Date, ($last + 1)
