using System.Text.RegularExpressions;
using Atualizador.Windows.Models;

namespace Atualizador.Windows.Services;

public sealed class WingetService : IPackageUpdateService
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(45);
    private readonly IProcessExecutor _processExecutor;
    private readonly IExecutableLocator _locator;

    public WingetService(
        IProcessExecutor? processExecutor = null,
        IExecutableLocator? locator = null)
    {
        _processExecutor = processExecutor ?? new SystemProcessExecutor();
        _locator = locator ?? new WingetLocator();
    }

    public async Task<WingetDiscoveryResult> DiscoverUpdatesAsync(
        bool includeUnknown = false,
        CancellationToken cancellationToken = default)
    {
        var wingetPath = _locator.Locate();
        if (wingetPath is null)
        {
            return Result(
                WingetDiscoveryStatus.WinGetUnavailable,
                "O WinGet não foi localizado. Instale ou repare o App Installer e tente novamente.");
        }

        var versionResult = await ExecuteAsync(wingetPath, ["--version"], cancellationToken);
        if (!versionResult.Started || versionResult.Canceled || versionResult.TimedOut)
        {
            return Result(
                WingetDiscoveryStatus.ExecutionFailed,
                versionResult.ErrorMessage ?? "Não foi possível iniciar o WinGet.",
                versionResult.ExitCode);
        }

        var sourceResult = await ExecuteAsync(
            wingetPath,
            ["source", "list", "--disable-interactivity"],
            cancellationToken);
        if (!sourceResult.Started || sourceResult.ExitCode != 0)
        {
            return Result(
                WingetDiscoveryStatus.SourceFailed,
                "Não foi possível consultar as fontes do WinGet.",
                sourceResult.ExitCode,
                versionResult.StandardOutput.Trim(),
                WingetSourceParser.Parse(sourceResult.StandardOutput));
        }

        var arguments = new List<string> { "list", "--upgrade-available", "--source", "winget", "--disable-interactivity" };
        if (includeUnknown)
        {
            arguments.Add("--include-unknown");
        }

        var updatesResult = await ExecuteAsync(wingetPath, arguments, cancellationToken);
        if (!updatesResult.Started || updatesResult.Canceled || updatesResult.TimedOut)
        {
            return Result(
                WingetDiscoveryStatus.ExecutionFailed,
                updatesResult.ErrorMessage ?? "Não foi possível descobrir as atualizações.",
                updatesResult.ExitCode,
                versionResult.StandardOutput.Trim(),
                WingetSourceParser.Parse(sourceResult.StandardOutput));
        }

        var parsed = WingetOutputParser.ParseUpgradeList(updatesResult.StandardOutput);
        var sources = WingetSourceParser.Parse(sourceResult.StandardOutput);
        var message = parsed.Packages.Count == 0
            ? "Nenhuma atualização disponível foi encontrada."
            : $"{parsed.Packages.Count} atualização(ões) encontrada(s).";
        var status = updatesResult.ExitCode == 0 && parsed.Warnings.Count == 0
            ? parsed.Packages.Count == 0 ? WingetDiscoveryStatus.NoUpdates : WingetDiscoveryStatus.Success
            : WingetDiscoveryStatus.Partial;

        return new(
            status,
            parsed.Packages,
            sources,
            versionResult.StandardOutput.Trim(),
            message,
            updatesResult.ExitCode,
            parsed.Warnings);
    }

    public async Task<PackageUpdateResult> UpgradeAsync(
        UpdatablePackage package,
        CancellationToken cancellationToken = default)
    {
        if (!IsSafeIdentifier(package.Id) || !IsSafeIdentifier(package.Source))
        {
            return new(package, PackageUpdateStatus.Ignored, "O ID ou a origem do pacote não foi validado.", null);
        }

        if (package.IsVersionUnknown)
        {
            return new(package, PackageUpdateStatus.Ignored, "A versão instalada é desconhecida.", null);
        }

        var wingetPath = _locator.Locate();
        if (wingetPath is null)
        {
            return new(package, PackageUpdateStatus.Failed, "O WinGet não foi localizado.", null);
        }

        var arguments = new[]
        {
            "upgrade",
            "--id", package.Id,
            "--exact",
            "--source", package.Source,
            "--silent",
            "--accept-source-agreements",
            "--accept-package-agreements",
            "--disable-interactivity"
        };
        var processResult = await ExecuteAsync(wingetPath, arguments, cancellationToken);

        if (processResult.Canceled)
        {
            return new(package, PackageUpdateStatus.Canceled, "A atualização foi cancelada.", null);
        }

        if (processResult.TimedOut)
        {
            return new(package, PackageUpdateStatus.Failed, "A atualização excedeu o tempo limite.", null);
        }

        if (!processResult.Started)
        {
            return new(package, PackageUpdateStatus.Failed, processResult.ErrorMessage ?? "Não foi possível iniciar o WinGet.", null);
        }

        return processResult.ExitCode == 0
            ? new(package, PackageUpdateStatus.Succeeded, "Atualização concluída.", processResult.ExitCode)
            : new(package, PackageUpdateStatus.Failed, SummarizeError(processResult.StandardError), processResult.ExitCode);
    }

    public async Task<WingetSearchResult> SearchAsync(
        string query,
        string source = "winget",
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
        {
            return new(WingetSearchStatus.NoPackagesFound, [], "Informe ao menos 2 caracteres para pesquisar.");
        }

        var wingetPath = _locator.Locate();
        if (wingetPath is null)
        {
            return new(
                WingetSearchStatus.WinGetUnavailable,
                [],
                "O WinGet não foi localizado. Instale ou repare o App Installer e tente novamente.");
        }

        var arguments = new List<string> { "search", query.Trim(), "--disable-interactivity" };
        if (!string.IsNullOrWhiteSpace(source) && !source.Equals("Todas", StringComparison.OrdinalIgnoreCase))
        {
            arguments.Add("--source");
            arguments.Add(source);
        }

        var result = await ExecuteAsync(wingetPath, arguments, cancellationToken);
        if (!result.Started || result.Canceled || result.TimedOut)
        {
            return new(
                WingetSearchStatus.ExecutionFailed,
                [],
                result.ErrorMessage ?? "A consulta ao catálogo do WinGet falhou.",
                result.ExitCode);
        }

        var packages = WingetOutputParser.ParseSearchResults(result.StandardOutput, source);
        if (packages.Count == 0)
        {
            return new(
                WingetSearchStatus.NoPackagesFound,
                [],
                "Nenhum aplicativo foi encontrado para o termo informado.",
                result.ExitCode);
        }

        return new(
            WingetSearchStatus.Success,
            packages,
            $"{packages.Count} aplicativo(s) encontrado(s).",
            result.ExitCode);
    }

    public async Task<PackageInstallResult> InstallAsync(
        InstallablePackage package,
        CancellationToken cancellationToken = default)
    {
        if (!IsSafeIdentifier(package.Id) || !IsSafeIdentifier(package.Source))
        {
            return new(package, PackageUpdateStatus.Failed, "O ID ou a origem do pacote não foi validado.", null);
        }

        var wingetPath = _locator.Locate();
        if (wingetPath is null)
        {
            return new(package, PackageUpdateStatus.Failed, "O WinGet não foi localizado.", null);
        }

        var arguments = new[]
        {
            "install",
            "--id", package.Id,
            "--exact",
            "--source", package.Source,
            "--silent",
            "--accept-source-agreements",
            "--accept-package-agreements",
            "--disable-interactivity"
        };

        var processResult = await ExecuteAsync(wingetPath, arguments, cancellationToken);

        if (processResult.Canceled)
        {
            return new(package, PackageUpdateStatus.Canceled, "A instalação foi cancelada.", null);
        }

        if (processResult.TimedOut)
        {
            return new(package, PackageUpdateStatus.Failed, "A instalação excedeu o tempo limite.", null);
        }

        if (!processResult.Started)
        {
            return new(package, PackageUpdateStatus.Failed, processResult.ErrorMessage ?? "Não foi possível iniciar o WinGet.", null);
        }

        if (processResult.ExitCode == 0)
        {
            return new(package, PackageUpdateStatus.Succeeded, "Instalação concluída com sucesso.", processResult.ExitCode);
        }

        var combinedOutput = $"{processResult.StandardError} {processResult.StandardOutput}".Trim();
        if (processResult.ExitCode is 5 or -1978335189 || combinedOutput.Contains("admin", StringComparison.OrdinalIgnoreCase) || combinedOutput.Contains("elevat", StringComparison.OrdinalIgnoreCase))
        {
            return new(package, PackageUpdateStatus.RequiresElevation, "A instalação exige privilégios de administrador (elevação administrativa).", processResult.ExitCode);
        }

        if (processResult.ExitCode is -1978335215 || combinedOutput.Contains("interactive", StringComparison.OrdinalIgnoreCase))
        {
            return new(package, PackageUpdateStatus.RequiresInteraction, "O instalador exige interação do usuário e não pôde ser executado em modo silencioso.", processResult.ExitCode);
        }

        return new(package, PackageUpdateStatus.Failed, SummarizeError(string.IsNullOrWhiteSpace(processResult.StandardError) ? processResult.StandardOutput : processResult.StandardError), processResult.ExitCode);
    }

    private async Task<ProcessExecutionResult> ExecuteAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken) =>
        await _processExecutor.ExecuteAsync(
            new ProcessExecutionRequest(fileName, arguments, DefaultTimeout),
            cancellationToken);

    private static WingetDiscoveryResult Result(
        WingetDiscoveryStatus status,
        string message,
        int? exitCode = null,
        string? version = null,
        IReadOnlyList<WingetSource>? sources = null) =>
        new(status, [], sources ?? [], version, message, exitCode, []);

    private static bool IsSafeIdentifier(string value) =>
        !string.IsNullOrWhiteSpace(value)
        && Regex.IsMatch(value, "\\A[A-Za-z0-9][A-Za-z0-9._-]*\\z", RegexOptions.CultureInvariant);

    private static string SummarizeError(string error)
    {
        var message = string.IsNullOrWhiteSpace(error)
            ? "O WinGet retornou um código de erro."
            : error.Trim();
        return message.Length <= 300 ? message : $"{message[..297]}...";
    }
}
