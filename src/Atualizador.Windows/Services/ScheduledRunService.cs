using Atualizador.Windows.Models;

namespace Atualizador.Windows.Services;

/// <summary>
/// Execução sem janela iniciada pela tarefa agendada (<c>Atualizador.Windows.exe --scheduled</c>).
/// <para>
/// Lê <c>schedule.json</c>, descobre as atualizações (ou usa os pacotes escolhidos), remove os
/// excluídos, atualiza um por um e grava o resultado no histórico.
/// </para>
/// <para>Códigos de saída: 0 = sucesso ou nada a fazer; 1 = falha ou cancelamento; 2 = outra execução já ativa.</para>
/// </summary>
public sealed class ScheduledRunService
{
    // Mutex nomeado: impede duas execuções agendadas ao mesmo tempo (ex.: a anterior ainda não terminou).
    private const string MutexName = "Atualizador.Windows.ScheduledUpdate.v1";
    private readonly ScheduleStore _scheduleStore;
    private readonly IPackageUpdateService _wingetService;
    private readonly HistoryStore _historyStore;
    private readonly ExcludedPackagesStore _excludedPackagesStore;

    public ScheduledRunService(
        ScheduleStore? scheduleStore = null,
        IPackageUpdateService? wingetService = null,
        HistoryStore? historyStore = null,
        ExcludedPackagesStore? excludedPackagesStore = null)
    {
        _scheduleStore = scheduleStore ?? new ScheduleStore();
        _wingetService = wingetService ?? new WingetService();
        _historyStore = historyStore ?? new HistoryStore();
        _excludedPackagesStore = excludedPackagesStore ?? new ExcludedPackagesStore();
    }

    public async Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        using var mutex = new Mutex(false, MutexName, out var createdNew);
        if (!createdNew || !mutex.WaitOne(TimeSpan.Zero))
        {
            return 2;
        }

        var preferences = _scheduleStore.Load();
        if (preferences.Mode == ScheduleMode.Desligado)
        {
            return 0;
        }

        var startedAt = DateTimeOffset.Now;
        IReadOnlyList<UpdatablePackage> packages;
        if (preferences.Scope == ScheduleScope.PacotesSelecionados)
        {
            packages = preferences.SelectedPackages;
        }
        else
        {
            var discovery = await _wingetService.DiscoverUpdatesAsync(cancellationToken: cancellationToken);
            if (discovery.Status is WingetDiscoveryStatus.WinGetUnavailable
                or WingetDiscoveryStatus.ExecutionFailed
                or WingetDiscoveryStatus.SourceFailed)
            {
                _historyStore.Append(new HistoryEntry(
                    Guid.NewGuid(),
                    startedAt,
                    DateTimeOffset.Now,
                    HistoryOperation.Descoberta,
                    "WinGet",
                    0,
                    HistoryEntryStatus.Falha,
                    discovery.Message,
                    []));
                return 1;
            }

            packages = discovery.Packages;
        }

        packages = packages.Where(package => !_excludedPackagesStore.IsExcluded(package)).ToArray();
        var results = new List<HistoryPackageResult>();
        foreach (var package in packages)
        {
            var result = await _wingetService.UpgradeAsync(package, cancellationToken);
            results.Add(new(package.Name, package.Id, package.Source, result.Status, result.Message));
        }

        var status = results.Any(result => result.Status == PackageUpdateStatus.Canceled)
            ? HistoryEntryStatus.Cancelado
            : results.Any(result => result.Status == PackageUpdateStatus.Failed)
                ? HistoryEntryStatus.Falha
                : results.Count == 0 ? HistoryEntryStatus.SemAtualizacoes : HistoryEntryStatus.Sucesso;
        _historyStore.Append(new HistoryEntry(
            Guid.NewGuid(),
            startedAt,
            DateTimeOffset.Now,
            HistoryOperation.Atualizacao,
            "WinGet",
            results.Count,
            status,
            $"Execução agendada processou {results.Count} pacote(s).",
            results));
        return status is HistoryEntryStatus.Falha or HistoryEntryStatus.Cancelado ? 1 : 0;
    }
}
