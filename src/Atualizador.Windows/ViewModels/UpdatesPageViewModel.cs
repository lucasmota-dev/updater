using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Data;
using Atualizador.Windows.Models;
using Atualizador.Windows.Services;

namespace Atualizador.Windows.ViewModels;

/// <summary>
/// Página Atualizações: consulta o WinGet, mostra os pacotes com versão nova (menos os excluídos
/// pelo usuário) e atualiza os pacotes marcados, um de cada vez, com progresso e cancelamento.
/// Toda consulta e toda atualização viram um registro no histórico.
/// </summary>
public sealed class UpdatesPageViewModel : PageViewModel, IDisposable
{
    private readonly IPackageUpdateService _wingetService;
    private readonly HistoryStore _historyStore;
    private readonly ExcludedPackagesStore _excludedPackagesStore;
    private readonly ListCollectionView _filteredPackages;
    private readonly IUserConfirmation _confirmation;
    private readonly NotificationService? _notificationService;
    private bool _isLoading;
    private bool _isError;
    private bool _isUpdating;
    private double _updateProgress;
    private int _completedUpdates;
    private string _currentPackageName = string.Empty;
    private string _statusMessage = "Clique em Buscar atualizações para consultar o WinGet.";
    private CancellationTokenSource? _updateCancellation;

    public UpdatesPageViewModel(
        IPackageUpdateService wingetService,
        HistoryStore? historyStore = null,
        ExcludedPackagesStore? excludedPackagesStore = null,
        IUserConfirmation? confirmation = null,
        NotificationService? notificationService = null)
        : base("Atualizações", "Veja quais aplicativos instalados têm uma versão mais nova disponível.", "\uE895")
    {
        _wingetService = wingetService;
        _historyStore = historyStore ?? new HistoryStore();
        _excludedPackagesStore = excludedPackagesStore ?? new ExcludedPackagesStore();
        _confirmation = confirmation ?? new MessageBoxUserConfirmation();
        _notificationService = notificationService;
        Packages = [];
        _filteredPackages = new ListCollectionView(Packages);
        _filteredPackages.SortDescriptions.Add(new SortDescription(nameof(UpdateItemViewModel.Name), ListSortDirection.Ascending));
        RefreshCommand = new AsyncDelegateCommand(RefreshAsync, () => !IsLoading && !IsUpdating);
        SelectAllCommand = new DelegateCommand(SelectAll, () => HasPackages && !IsLoading && !IsUpdating);
        ClearSelectionCommand = new DelegateCommand(ClearSelection, () => HasSelectedPackages && !IsLoading && !IsUpdating);
        UpdateSelectedCommand = new AsyncDelegateCommand(UpdateSelectedAsync, CanUpdateSelected);
        CancelUpdateCommand = new DelegateCommand(CancelUpdate, () => IsUpdating);
        ExcludeSelectedCommand = new DelegateCommand(ExcludeSelected, CanExcludeSelected);
    }

    public ObservableCollection<UpdateItemViewModel> Packages { get; }

    public ICollectionView FilteredPackages => _filteredPackages;

    public AsyncDelegateCommand RefreshCommand { get; }

    public DelegateCommand SelectAllCommand { get; }

    public DelegateCommand ClearSelectionCommand { get; }

    public AsyncDelegateCommand UpdateSelectedCommand { get; }

    public DelegateCommand CancelUpdateCommand { get; }

    public DelegateCommand ExcludeSelectedCommand { get; }

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (!SetProperty(ref _isLoading, value))
            {
                return;
            }

            RaiseCommandStates();
        }
    }

    public bool IsUpdating
    {
        get => _isUpdating;
        private set
        {
            if (!SetProperty(ref _isUpdating, value))
            {
                return;
            }

            RaiseCommandStates();
        }
    }

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

    public int CompletedUpdates
    {
        get => _completedUpdates;
        private set => SetProperty(ref _completedUpdates, value);
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

    public string EmptyStateMessage => Packages.Count == 0
        ? "Nenhuma atualização carregada. Clique em Buscar atualizações para consultar o WinGet."
        : "Nenhuma atualização corresponde ao filtro selecionado.";

    public int SelectedCount => Packages.Count(package => package.IsSelected);

    public IReadOnlyList<UpdatablePackage> SelectedPackages =>
        Packages.Where(package => package.IsSelected).Select(package => package.Package).ToArray();

    /// <summary>
    /// Botão "Buscar atualizações" (e item "Verificar atualizações" da bandeja): consulta o WinGet,
    /// remove os pacotes excluídos, substitui a lista e registra o resultado no histórico.
    /// </summary>
    private async Task RefreshAsync()
    {
        var startedAt = DateTimeOffset.Now;
        IsLoading = true;
        IsError = false;
        StatusMessage = "Consultando o WinGet…";
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
                _notificationService?.Publish(new(NotificationKind.Informacao, "Atualizações encontradas", result.Message));
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
                Guid.NewGuid(),
                startedAt,
                DateTimeOffset.Now,
                HistoryOperation.Descoberta,
                "WinGet",
                0,
                HistoryEntryStatus.Falha,
                StatusMessage,
                []));
        }
        finally
        {
            IsLoading = false;
            UpdateCommandStates();
        }
    }

    /// <summary>
    /// Pede confirmação e atualiza os pacotes marcados em sequência. Revalida a lista de exclusões
    /// antes de cada pacote, porque ela pode ter mudado depois da consulta.
    /// </summary>
    private async Task UpdateSelectedAsync()
    {
        var selectedPackages = Packages.Where(package => package.IsSelected).ToArray();
        if (selectedPackages.Length == 0)
        {
            return;
        }

        if (!_confirmation.Confirm(
                "Confirmar atualização",
                $"Deseja atualizar {selectedPackages.Length} aplicativo(s) selecionado(s)?"))
        {
            StatusMessage = "A atualização não foi iniciada.";
            return;
        }

        var startedAt = DateTimeOffset.Now;
        IsUpdating = true;
        IsError = false;
        CompletedUpdates = 0;
        UpdateProgress = 0;
        _updateCancellation = new CancellationTokenSource();
        try
        {
            foreach (var package in selectedPackages)
            {
                CurrentPackageName = package.Name;
                if (_excludedPackagesStore.IsExcluded(package.Package))
                {
                    package.Status = PackageUpdateStatus.Ignored;
                    package.StatusMessage = "Excluído da atualização antes da execução.";
                    CompletedUpdates++;
                    UpdateProgress = (double)CompletedUpdates / selectedPackages.Length * 100;
                    continue;
                }

                package.Status = PackageUpdateStatus.InProgress;
                package.StatusMessage = "Instalação em andamento…";
                StatusMessage = $"Atualizando {package.Name}…";
                var result = await _wingetService.UpgradeAsync(package.Package, _updateCancellation.Token);
                package.Status = result.Status;
                package.StatusMessage = result.Message;
                CompletedUpdates++;
                UpdateProgress = (double)CompletedUpdates / selectedPackages.Length * 100;

                if (result.Status == PackageUpdateStatus.Canceled)
                {
                    foreach (var remaining in selectedPackages.Skip(CompletedUpdates))
                    {
                        remaining.Status = PackageUpdateStatus.Canceled;
                        remaining.StatusMessage = "Não iniciado porque a operação foi cancelada.";
                    }

                    break;
                }
            }

            var hasFailures = selectedPackages.Any(package => package.Status == PackageUpdateStatus.Failed);
            StatusMessage = _updateCancellation.IsCancellationRequested
                ? "A atualização foi cancelada. O instalador externo pode não ter revertido alterações."
                : hasFailures
                    ? $"{CompletedUpdates} aplicativo(s) processado(s); pelo menos um falhou."
                    : $"{CompletedUpdates} aplicativo(s) processado(s).";
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
            _historyStore.Append(CreateUpdateHistory(selectedPackages, startedAt));
            _updateCancellation?.Dispose();
            _updateCancellation = null;
            IsUpdating = false;
            CurrentPackageName = string.Empty;
        }
    }

    private bool CanUpdateSelected() => HasSelectedPackages && !IsLoading && !IsUpdating;

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
        OnPropertyChanged(nameof(HasSelectedPackages));
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(EmptyStateMessage));
        UpdateCommandStates();
    }

    private void SelectAll()
    {
        foreach (var package in _filteredPackages.Cast<UpdateItemViewModel>())
        {
            package.IsSelected = true;
        }

        UpdateCommandStates();
    }

    private void ClearSelection()
    {
        foreach (var package in Packages)
        {
            package.IsSelected = false;
        }

        UpdateCommandStates();
    }

    private void OnPackagePropertyChanged(object? sender, PropertyChangedEventArgs eventArgs)
    {
        if (eventArgs.PropertyName == nameof(UpdateItemViewModel.IsSelected))
        {
            UpdateCommandStates();
        }
    }

    private void UpdateCommandStates()
    {
        OnPropertyChanged(nameof(HasSelectedPackages));
        OnPropertyChanged(nameof(SelectedCount));
        RaiseCommandStates();
    }

    private void RaiseCommandStates()
    {
        RefreshCommand.RaiseCanExecuteChanged();
        SelectAllCommand.RaiseCanExecuteChanged();
        ClearSelectionCommand.RaiseCanExecuteChanged();
        UpdateSelectedCommand.RaiseCanExecuteChanged();
        CancelUpdateCommand.RaiseCanExecuteChanged();
        ExcludeSelectedCommand.RaiseCanExecuteChanged();
    }

    private bool CanExcludeSelected() => HasSelectedPackages && !IsLoading && !IsUpdating;

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

    private static HistoryEntry CreateUpdateHistory(
        UpdateItemViewModel[] packages,
        DateTimeOffset startedAt)
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
