using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Data;
using Atualizador.Windows.Models;
using Atualizador.Windows.Services;

namespace Atualizador.Windows.ViewModels;

/// <summary>
/// Página Atualizações: consulta o WinGet, mostra os pacotes com versão nova (menos os excluídos
/// pelo usuário) e atualiza os pacotes marcados, ou todos, um de cada vez, com progresso e cancelamento.
/// Toda consulta e toda atualização viram um registro no histórico.
/// </summary>
public sealed class UpdatesPageViewModel : PageViewModel, IDisposable
{
    /// <summary>Ao voltar para a página, a lista é consultada de novo se tiver mais que isso.</summary>
    public static readonly TimeSpan AutoRefreshAge = TimeSpan.FromMinutes(10);

    private readonly IPackageUpdateService _wingetService;
    private readonly HistoryStore _historyStore;
    private readonly ExcludedPackagesStore _excludedPackagesStore;
    private readonly ListCollectionView _filteredPackages;
    private readonly IUserConfirmation _confirmation;
    private readonly NotificationService? _notificationService;
    private readonly PackageOperationGate _operationGate;
    private bool _isLoading;
    private bool _isError;
    private bool _isUpdating;
    private double _updateProgress;
    private string _currentPackageName = string.Empty;
    private string _statusMessage = "Clique em Buscar atualizações para consultar o WinGet.";
    private DateTimeOffset? _lastRefreshAt;
    private CancellationTokenSource? _updateCancellation;

    public UpdatesPageViewModel(
        IPackageUpdateService wingetService,
        HistoryStore? historyStore = null,
        ExcludedPackagesStore? excludedPackagesStore = null,
        IUserConfirmation? confirmation = null,
        NotificationService? notificationService = null,
        PackageOperationGate? operationGate = null)
        : base("Atualizações", "Veja quais aplicativos instalados têm uma versão mais nova disponível.", "")
    {
        _wingetService = wingetService;
        _historyStore = historyStore ?? new HistoryStore();
        _excludedPackagesStore = excludedPackagesStore ?? new ExcludedPackagesStore();
        _confirmation = confirmation ?? new MessageBoxUserConfirmation();
        _notificationService = notificationService;
        _operationGate = operationGate ?? new PackageOperationGate();
        Packages = [];
        _filteredPackages = new ListCollectionView(Packages);
        _filteredPackages.SortDescriptions.Add(new SortDescription(nameof(UpdateItemViewModel.Name), ListSortDirection.Ascending));
        RefreshCommand = new AsyncDelegateCommand(RefreshAsync, () => !IsBusy);
        UpdateSelectedCommand = new AsyncDelegateCommand(UpdateSelectedAsync, () => HasSelectedPackages && !IsBusy);
        UpdateAllCommand = new AsyncDelegateCommand(UpdateAllAsync, () => HasPackages && !IsBusy);
        CancelUpdateCommand = new DelegateCommand(CancelUpdate, () => IsUpdating);
        ExcludeSelectedCommand = new DelegateCommand(ExcludeSelected, () => HasSelectedPackages && !IsBusy);
    }

    public ObservableCollection<UpdateItemViewModel> Packages { get; }

    public ICollectionView FilteredPackages => _filteredPackages;

    /// <summary>Botão "Buscar atualizações" (e item "Verificar atualizações" da bandeja).</summary>
    public AsyncDelegateCommand RefreshCommand { get; }

    public AsyncDelegateCommand UpdateSelectedCommand { get; }

    /// <summary>Atualiza todos os pacotes da lista, um por um. Nunca usa <c>winget upgrade --all</c>, que ignoraria as exclusões.</summary>
    public AsyncDelegateCommand UpdateAllCommand { get; }

    public DelegateCommand CancelUpdateCommand { get; }

    public DelegateCommand ExcludeSelectedCommand { get; }

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (SetProperty(ref _isLoading, value))
            {
                OnPropertyChanged(nameof(IsBusy));
                RaiseCommandStates();
            }
        }
    }

    public bool IsUpdating
    {
        get => _isUpdating;
        private set
        {
            if (SetProperty(ref _isUpdating, value))
            {
                OnPropertyChanged(nameof(IsBusy));
                RaiseCommandStates();
            }
        }
    }

    /// <summary>Consulta ou atualização em andamento: bloqueia a seleção e os botões de ação.</summary>
    public bool IsBusy => IsLoading || IsUpdating;

    public bool IsError
    {
        get => _isError;
        private set => SetProperty(ref _isError, value);
    }

    public double UpdateProgress
    {
        get => _updateProgress;
        private set => SetProperty(ref _updateProgress, value);
    }

    public string CurrentPackageName
    {
        get => _currentPackageName;
        private set => SetProperty(ref _currentPackageName, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public bool HasPackages => _filteredPackages.Cast<UpdateItemViewModel>().Any();

    public bool HasSelectedPackages => Packages.Any(package => package.IsSelected);

    public string EmptyStateMessage => IsLoading
        ? "Consultando o WinGet…"
        : Packages.Count == 0
            ? "Nenhuma atualização encontrada. Clique em Buscar atualizações para consultar de novo."
            : "Nenhuma atualização corresponde ao filtro selecionado.";

    public int SelectedCount => Packages.Count(package => package.IsSelected);

    /// <summary>
    /// Caixa "marcar/desmarcar todos" do cabeçalho da lista: marcada se todos estão marcados,
    /// desmarcada se nenhum, e indeterminada (null) se só alguns.
    /// </summary>
    public bool? AreAllSelected
    {
        get
        {
            var selected = SelectedCount;
            return selected == 0 ? false : selected == Packages.Count ? true : null;
        }
        set
        {
            var select = value == true;
            foreach (var package in _filteredPackages.Cast<UpdateItemViewModel>())
            {
                package.IsSelected = select;
            }

            UpdateSelectionState();
        }
    }

    public IReadOnlyList<UpdatablePackage> SelectedPackages =>
        Packages.Where(package => package.IsSelected).Select(package => package.Package).ToArray();

    /// <summary>
    /// Busca automática ao abrir a página: consulta se ainda não consultou nesta sessão ou se a
    /// última consulta tem mais de <see cref="AutoRefreshAge"/>. Não faz nada durante outra operação.
    /// </summary>
    public Task RefreshIfStaleAsync()
    {
        var isStale = _lastRefreshAt is null || DateTimeOffset.Now - _lastRefreshAt > AutoRefreshAge;
        return isStale && !IsBusy ? RefreshAsync() : Task.CompletedTask;
    }

    /// <summary>
    /// Consulta o WinGet, remove os pacotes excluídos, substitui a lista e registra o resultado no histórico.
    /// </summary>
    private async Task RefreshAsync()
    {
        var startedAt = DateTimeOffset.Now;
        _lastRefreshAt = startedAt;
        IsLoading = true;
        IsError = false;
        StatusMessage = "Consultando o WinGet…";
        OnPropertyChanged(nameof(EmptyStateMessage));
        try
        {
            var result = await _wingetService.DiscoverUpdatesAsync();
            var visiblePackages = _excludedPackagesStore.Filter(result.Packages);
            var displayResult = result with
            {
                Packages = visiblePackages,
                Status = visiblePackages.Count == 0 && result.Packages.Count > 0
                    ? WingetDiscoveryStatus.NoUpdates
                    : result.Status,
                Message = visiblePackages.Count == result.Packages.Count
                    ? result.Message
                    : visiblePackages.Count == 0
                        ? "Nenhuma atualização elegível foi encontrada."
                        : $"{visiblePackages.Count} atualização(ões) elegível(is) encontrada(s)."
            };
            _historyStore.Append(CreateDiscoveryHistory(displayResult, startedAt));
            ReplacePackages(displayResult.Packages);
            IsError = displayResult.Status is WingetDiscoveryStatus.ExecutionFailed
                or WingetDiscoveryStatus.SourceFailed
                or WingetDiscoveryStatus.WinGetUnavailable;
            StatusMessage = displayResult.Message;
            if (displayResult.Status == WingetDiscoveryStatus.Success)
            {
                _notificationService?.Publish(new(NotificationKind.Informacao, "Atualizações encontradas", displayResult.Message));
            }

            if (displayResult.Warnings.Count > 0)
            {
                StatusMessage += " Alguns registros não puderam ser exibidos.";
            }
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "A consulta foi cancelada.";
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException)
        {
            IsError = true;
            StatusMessage = $"Não foi possível consultar o WinGet: {exception.Message}";
            _notificationService?.Publish(new(NotificationKind.Erro, "Falha na consulta", StatusMessage));
            _historyStore.Append(new HistoryEntry(
                Guid.NewGuid(), startedAt, DateTimeOffset.Now, HistoryOperation.Descoberta, "WinGet",
                0, HistoryEntryStatus.Falha, StatusMessage, []));
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(EmptyStateMessage));
            UpdateSelectionState();
        }
    }

    private Task UpdateSelectedAsync()
    {
        var selected = Packages.Where(package => package.IsSelected).ToArray();
        return UpdatePackagesAsync(selected, $"Deseja atualizar {selected.Length} aplicativo(s) selecionado(s)?");
    }

    private Task UpdateAllAsync()
    {
        var all = _filteredPackages.Cast<UpdateItemViewModel>().ToArray();
        foreach (var package in all)
        {
            package.IsSelected = true;
        }

        UpdateSelectionState();
        return UpdatePackagesAsync(all, $"Deseja atualizar todos os {all.Length} aplicativo(s) da lista?");
    }

    /// <summary>
    /// Pede confirmação e atualiza os pacotes em sequência. Revalida a lista de exclusões antes de
    /// cada pacote, porque ela pode ter mudado depois da consulta.
    /// </summary>
    private async Task UpdatePackagesAsync(UpdateItemViewModel[] packages, string confirmationMessage)
    {
        if (packages.Length == 0)
        {
            return;
        }

        if (!_confirmation.Confirm("Confirmar atualização", confirmationMessage))
        {
            StatusMessage = "A atualização não foi iniciada.";
            return;
        }

        var startedAt = DateTimeOffset.Now;
        IsUpdating = true;
        IsError = false;
        UpdateProgress = 0;
        var completed = 0;
        _updateCancellation = new CancellationTokenSource();
        try
        {
            foreach (var package in packages)
            {
                CurrentPackageName = package.Name;
                if (_excludedPackagesStore.IsExcluded(package.Package))
                {
                    package.Status = PackageUpdateStatus.Ignored;
                    package.StatusMessage = "Excluído da atualização antes da execução.";
                }
                else
                {
                    package.Status = PackageUpdateStatus.InProgress;
                    package.StatusMessage = "Instalação em andamento…";
                    StatusMessage = $"Atualizando {package.Name} ({completed + 1} de {packages.Length})…";
                    PackageUpdateResult result;
                    if (_operationGate.IsBusy)
                    {
                        package.StatusMessage = "Aguardando a instalação em andamento terminar…";
                    }

                    // Uma instalação ou atualização por vez no app inteiro (veja PackageOperationGate).
                    using (await _operationGate.EnterAsync(_updateCancellation.Token))
                    {
                        package.StatusMessage = "Instalação em andamento…";
                        result = await _wingetService.UpgradeAsync(package.Package, _updateCancellation.Token);

                        // Instalador que exige administrador: oferece repetir só este pacote com o aviso do UAC.
                        if (result.Status == PackageUpdateStatus.RequiresElevation && _confirmation.Confirm(
                                "Permissão de administrador",
                                $"A atualização de {package.Name} exige permissão de administrador.\n\n"
                                + "Tentar de novo como administrador? O Windows vai mostrar o aviso de permissão (UAC) só para este aplicativo."))
                        {
                            package.StatusMessage = "Aguardando a permissão de administrador…";
                            result = await _wingetService.UpgradeElevatedAsync(package.Package, _updateCancellation.Token);
                        }
                    }

                    package.Status = result.Status;
                    package.StatusMessage = result.Message;
                }

                completed++;
                UpdateProgress = (double)completed / packages.Length * 100;

                if (package.Status == PackageUpdateStatus.Canceled)
                {
                    foreach (var remaining in packages.Skip(completed))
                    {
                        remaining.Status = PackageUpdateStatus.Canceled;
                        remaining.StatusMessage = "Não iniciado porque a operação foi cancelada.";
                    }

                    break;
                }
            }

            var hasFailures = packages.Any(package => package.Status == PackageUpdateStatus.Failed);
            StatusMessage = _updateCancellation.IsCancellationRequested
                ? "A atualização foi cancelada. O instalador externo pode não ter revertido alterações."
                : hasFailures
                    ? $"{completed} aplicativo(s) processado(s); pelo menos um falhou."
                    : $"{completed} aplicativo(s) processado(s).";
            _notificationService?.Publish(new(
                _updateCancellation.IsCancellationRequested
                    ? NotificationKind.Informacao
                    : hasFailures ? NotificationKind.Erro : NotificationKind.Sucesso,
                "Atualização finalizada",
                StatusMessage));
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "A atualização foi cancelada. O instalador externo pode não ter revertido alterações.";
        }
        finally
        {
            _historyStore.Append(CreateUpdateHistory(packages, startedAt));
            _updateCancellation?.Dispose();
            _updateCancellation = null;
            IsUpdating = false;
            CurrentPackageName = string.Empty;
        }
    }

    private void CancelUpdate() => _updateCancellation?.Cancel();

    private void ReplacePackages(IReadOnlyList<UpdatablePackage> packages)
    {
        foreach (var package in Packages)
        {
            package.PropertyChanged -= OnPackagePropertyChanged;
        }

        Packages.Clear();
        foreach (var package in packages)
        {
            var item = new UpdateItemViewModel(package);
            item.PropertyChanged += OnPackagePropertyChanged;
            Packages.Add(item);
        }

        _filteredPackages.Refresh();
        OnPropertyChanged(nameof(HasPackages));
        OnPropertyChanged(nameof(EmptyStateMessage));
        UpdateSelectionState();
    }

    private void OnPackagePropertyChanged(object? sender, PropertyChangedEventArgs eventArgs)
    {
        if (eventArgs.PropertyName == nameof(UpdateItemViewModel.IsSelected))
        {
            UpdateSelectionState();
        }
    }

    /// <summary>Avisa a tela de tudo que depende da seleção: contador, caixa "todos" e botões.</summary>
    private void UpdateSelectionState()
    {
        OnPropertyChanged(nameof(HasSelectedPackages));
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(AreAllSelected));
        RaiseCommandStates();
    }

    private void RaiseCommandStates()
    {
        RefreshCommand.RaiseCanExecuteChanged();
        UpdateSelectedCommand.RaiseCanExecuteChanged();
        UpdateAllCommand.RaiseCanExecuteChanged();
        CancelUpdateCommand.RaiseCanExecuteChanged();
        ExcludeSelectedCommand.RaiseCanExecuteChanged();
    }

    private void ExcludeSelected()
    {
        var selected = Packages.Where(package => package.IsSelected).ToArray();
        if (selected.Length == 0 || !_confirmation.Confirm(
                "Excluir da atualização",
                $"Excluir {selected.Length} aplicativo(s) selecionado(s) das próximas atualizações?"))
        {
            return;
        }

        _excludedPackagesStore.Add(selected.Select(package => package.Package));
        ReplacePackages(Packages
            .Where(package => !package.IsSelected)
            .Select(package => package.Package)
            .ToArray());
        StatusMessage = $"{selected.Length} aplicativo(s) excluído(s) da atualização.";
    }

    private static HistoryEntry CreateDiscoveryHistory(WingetDiscoveryResult result, DateTimeOffset startedAt) =>
        new(
            Guid.NewGuid(),
            startedAt,
            DateTimeOffset.Now,
            HistoryOperation.Descoberta,
            "WinGet",
            result.Packages.Count,
            result.Status switch
            {
                WingetDiscoveryStatus.Success => HistoryEntryStatus.Sucesso,
                WingetDiscoveryStatus.NoUpdates => HistoryEntryStatus.SemAtualizacoes,
                WingetDiscoveryStatus.Partial => HistoryEntryStatus.Parcial,
                _ => HistoryEntryStatus.Falha
            },
            result.Message,
            result.Packages.Select(package => new HistoryPackageResult(
                package.Name,
                package.Id,
                package.Source,
                PackageUpdateStatus.NotStarted,
                "Atualização disponível.")).ToArray());

    private static HistoryEntry CreateUpdateHistory(UpdateItemViewModel[] packages, DateTimeOffset startedAt)
    {
        var status = packages.Any(package => package.Status == PackageUpdateStatus.Canceled)
            ? HistoryEntryStatus.Cancelado
            : packages.Any(package => package.Status == PackageUpdateStatus.Failed)
                ? HistoryEntryStatus.Falha
                : packages.Any(package => package.Status == PackageUpdateStatus.Ignored)
                    ? HistoryEntryStatus.Parcial
                    : HistoryEntryStatus.Sucesso;
        return new(
            Guid.NewGuid(),
            startedAt,
            DateTimeOffset.Now,
            HistoryOperation.Atualizacao,
            "WinGet",
            packages.Length,
            status,
            $"{packages.Length} aplicativo(s) selecionado(s).",
            packages.Select(package => new HistoryPackageResult(
                package.Name,
                package.Id,
                package.Source,
                package.Status,
                package.StatusMessage)).ToArray());
    }

    public void Dispose()
    {
        _updateCancellation?.Cancel();
        _updateCancellation?.Dispose();
        _updateCancellation = null;
        foreach (var package in Packages)
        {
            package.PropertyChanged -= OnPackagePropertyChanged;
        }
    }
}

/// <summary>Uma linha da lista de atualizações: o pacote, a seleção e o estado da atualização.</summary>
public sealed class UpdateItemViewModel(UpdatablePackage package) : ViewModelBase
{
    private bool _isSelected;
    private PackageUpdateStatus _status = PackageUpdateStatus.NotStarted;
    private string _statusMessage = string.Empty;

    public UpdatablePackage Package { get; } = package;

    public string Name { get; } = package.Name;

    public string Id { get; } = package.Id;

    public string InstalledVersion { get; } = package.InstalledVersion ?? "Desconhecida";

    public string AvailableVersion { get; } = package.AvailableVersion ?? "Desconhecida";

    public string Source { get; } = package.Source;

    public bool IsVersionUnknown { get; } = package.IsVersionUnknown;

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public PackageUpdateStatus Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }
}
