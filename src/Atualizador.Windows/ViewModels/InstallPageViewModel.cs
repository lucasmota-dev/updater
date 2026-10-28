using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using Atualizador.Windows.Models;
using Atualizador.Windows.Services;

namespace Atualizador.Windows.ViewModels;

public sealed class InstallItemViewModel : ViewModelBase
{
    private bool _isSelected;
    private PackageUpdateStatus _status = PackageUpdateStatus.NotStarted;
    private string _statusMessage = "Pronto para instalar";

    public InstallItemViewModel(InstallablePackage package)
    {
        Package = package;
    }

    public InstallablePackage Package { get; }

    public string Name => Package.Name;

    public string Id => Package.Id;

    public string Version => Package.Version;

    public string Source => Package.Source;

    public string Publisher => Package.Publisher;

    public string Match => Package.Match;

    public string Glyph => "📦";

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

public sealed class InstallPageViewModel : PageViewModel, IDisposable
{
    private readonly IPackageUpdateService _wingetService;
    private readonly HistoryStore _historyStore;
    private readonly IUserConfirmation _confirmation;
    private readonly NotificationService? _notificationService;
    private readonly ListCollectionView _filteredPackages;
    private CancellationTokenSource? _cancellationTokenSource;
    private string _searchQuery = string.Empty;
    private string _filterText = string.Empty;
    private string _statusMessage = "Digite o nome ou ID de um aplicativo e clique em Pesquisar.";
    private string _currentPackageName = string.Empty;
    private bool _isSearching;
    private bool _isInstalling;
    private bool _isError;
    private double _installProgress;
    private int _completedCount;

    public InstallPageViewModel(
        IPackageUpdateService wingetService,
        HistoryStore? historyStore = null,
        IUserConfirmation? confirmation = null,
        NotificationService? notificationService = null)
        : base("Instalar", "Pesquise e instale aplicativos disponíveis no catálogo do WinGet.", "📥")
    {
        _wingetService = wingetService;
        _historyStore = historyStore ?? new HistoryStore();
        _confirmation = confirmation ?? new MessageBoxUserConfirmation();
        _notificationService = notificationService;
        Packages = [];
        _filteredPackages = new ListCollectionView(Packages)
        {
            Filter = FilterPackageItem
        };

        SearchCommand = new AsyncDelegateCommand(SearchAsync, CanSearch);
        SelectAllCommand = new DelegateCommand(SelectAll, () => HasPackages && !IsInstalling);
        ClearSelectionCommand = new DelegateCommand(ClearSelection, () => HasSelectedPackages && !IsInstalling);
        InstallSelectedCommand = new AsyncDelegateCommand(InstallSelectedAsync, CanInstallSelected);
        CancelInstallCommand = new DelegateCommand(CancelInstall, () => IsInstalling);
    }

    public ObservableCollection<InstallItemViewModel> Packages { get; }

    public ICollectionView FilteredPackages => _filteredPackages;

    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (SetProperty(ref _searchQuery, value))
            {
                SearchCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string FilterText
    {
        get => _filterText;
        set
        {
            if (SetProperty(ref _filterText, value))
            {
                _filteredPackages.Refresh();
                OnPropertyChanged(nameof(HasPackages));
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public string CurrentPackageName
    {
        get => _currentPackageName;
        set => SetProperty(ref _currentPackageName, value);
    }

    public bool IsSearching
    {
        get => _isSearching;
        private set
        {
            if (SetProperty(ref _isSearching, value))
            {
                RaiseCommandStates();
            }
        }
    }

    public bool IsInstalling
    {
        get => _isInstalling;
        private set
        {
            if (SetProperty(ref _isInstalling, value))
            {
                RaiseCommandStates();
            }
        }
    }

    public bool IsError
    {
        get => _isError;
        private set => SetProperty(ref _isError, value);
    }

    public double InstallProgress
    {
        get => _installProgress;
        private set => SetProperty(ref _installProgress, value);
    }

    public int SelectedCount => Packages.Count(package => package.IsSelected);

    public bool HasSelectedPackages => SelectedCount > 0;

    public bool HasPackages => _filteredPackages.Cast<object>().Any();

    public AsyncDelegateCommand SearchCommand { get; }

    public DelegateCommand SelectAllCommand { get; }

    public DelegateCommand ClearSelectionCommand { get; }

    public AsyncDelegateCommand InstallSelectedCommand { get; }

    public DelegateCommand CancelInstallCommand { get; }

    public async Task SearchAsync()
    {
        if (IsSearching || IsInstalling || string.IsNullOrWhiteSpace(SearchQuery))
        {
            return;
        }

        IsSearching = true;
        IsError = false;
        StatusMessage = $"Pesquisando por '{SearchQuery.Trim()}' no catálogo do WinGet…";
        CurrentPackageName = string.Empty;

        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            var result = await _wingetService.SearchAsync(
                SearchQuery.Trim(),
                "winget",
                _cancellationTokenSource.Token);

            ReplacePackages(result.Packages);
            IsError = result.Status is WingetSearchStatus.ExecutionFailed or WingetSearchStatus.WinGetUnavailable;
            StatusMessage = result.Message;
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "A pesquisa foi cancelada.";
        }
        finally
        {
            IsSearching = false;
        }
    }

    public async Task InstallSelectedAsync()
    {
        var selected = Packages.Where(package => package.IsSelected).ToArray();
        if (selected.Length == 0 || IsInstalling)
        {
            return;
        }

        var packageNames = string.Join(", ", selected.Select(package => package.Name));
        if (!_confirmation.Confirm(
                "Instalar aplicativos",
                $"Deseja instalar {selected.Length} aplicativo(s) selecionado(s)?\n\n{packageNames}\n\nAtenção: a fonte oficial winget é priorizada. A presença no catálogo não garante segurança absoluta."))
        {
            return;
        }

        IsInstalling = true;
        IsError = false;
        _completedCount = 0;
        InstallProgress = 0;
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = new CancellationTokenSource();

        var startedAt = DateTimeOffset.Now;
        var results = new List<HistoryPackageResult>();

        try
        {
            foreach (var item in selected)
            {
                if (_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    item.Status = PackageUpdateStatus.Canceled;
                    item.StatusMessage = "Instalação cancelada.";
                    results.Add(new(item.Name, item.Id, item.Source, PackageUpdateStatus.Canceled, item.StatusMessage));
                    continue;
                }

                CurrentPackageName = $"Instalando {item.Name} ({item.Id})…";
                StatusMessage = $"Processando {item.Name}…";
                item.Status = PackageUpdateStatus.InProgress;
                item.StatusMessage = "Instalação em andamento…";

                var result = await _wingetService.InstallAsync(item.Package, _cancellationTokenSource.Token);
                item.Status = result.Status;
                item.StatusMessage = result.Message;
                results.Add(new(item.Name, item.Id, item.Source, result.Status, result.Message));

                _completedCount++;
                InstallProgress = (double)_completedCount / selected.Length * 100;
            }

            var successCount = results.Count(r => r.Status == PackageUpdateStatus.Succeeded);
            var failCount = results.Count(r => r.Status != PackageUpdateStatus.Succeeded);
            var finalStatus = failCount == 0
                ? HistoryEntryStatus.Sucesso
                : successCount > 0
                    ? HistoryEntryStatus.Parcial
                    : HistoryEntryStatus.Falha;

            StatusMessage = $"Instalação concluída: {successCount} instalado(s) com sucesso, {failCount} pendência(s).";
            CurrentPackageName = string.Empty;

            var historyEntry = new HistoryEntry(
                Guid.NewGuid(),
                startedAt,
                DateTimeOffset.Now,
                HistoryOperation.Instalacao,
                "winget",
                results.Count,
                finalStatus,
                StatusMessage,
                results);

            _historyStore.Append(historyEntry);
            _notificationService?.Publish(new(
                failCount == 0 ? NotificationKind.Sucesso : NotificationKind.Informacao,
                "Instalação finalizada",
                StatusMessage));
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Instalações canceladas pelo usuário.";
            CurrentPackageName = string.Empty;
        }
        finally
        {
            IsInstalling = false;
        }
    }

    private void CancelInstall()
    {
        if (!IsInstalling || _cancellationTokenSource is null || _cancellationTokenSource.IsCancellationRequested)
        {
            return;
        }

        StatusMessage = "Cancelando instalação em andamento…";
        _cancellationTokenSource.Cancel();
    }

    private bool CanSearch() => !string.IsNullOrWhiteSpace(SearchQuery) && !IsSearching && !IsInstalling;

    private bool CanInstallSelected() => HasSelectedPackages && !IsSearching && !IsInstalling;

    private void SelectAll()
    {
        foreach (var package in _filteredPackages.Cast<InstallItemViewModel>())
        {
            package.IsSelected = true;
        }

        RaiseSelectionChanged();
    }

    private void ClearSelection()
    {
        foreach (var package in Packages)
        {
            package.IsSelected = false;
        }

        RaiseSelectionChanged();
    }

    private void ReplacePackages(IEnumerable<InstallablePackage> packages)
    {
        foreach (var existing in Packages)
        {
            existing.PropertyChanged -= OnPackagePropertyChanged;
        }

        Packages.Clear();
        foreach (var package in packages)
        {
            var item = new InstallItemViewModel(package);
            item.PropertyChanged += OnPackagePropertyChanged;
            Packages.Add(item);
        }

        _filteredPackages.Refresh();
        RaiseSelectionChanged();
        OnPropertyChanged(nameof(HasPackages));
    }

    private void OnPackagePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(InstallItemViewModel.IsSelected))
        {
            RaiseSelectionChanged();
        }
    }

    private void RaiseSelectionChanged()
    {
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(HasSelectedPackages));
        InstallSelectedCommand.RaiseCanExecuteChanged();
        ClearSelectionCommand.RaiseCanExecuteChanged();
    }

    private void RaiseCommandStates()
    {
        SearchCommand.RaiseCanExecuteChanged();
        SelectAllCommand.RaiseCanExecuteChanged();
        ClearSelectionCommand.RaiseCanExecuteChanged();
        InstallSelectedCommand.RaiseCanExecuteChanged();
        CancelInstallCommand.RaiseCanExecuteChanged();
    }

    private bool FilterPackageItem(object item)
    {
        if (item is not InstallItemViewModel package)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(FilterText))
        {
            return true;
        }

        var filter = FilterText.Trim();
        return package.Name.Contains(filter, StringComparison.CurrentCultureIgnoreCase)
            || package.Id.Contains(filter, StringComparison.OrdinalIgnoreCase)
            || package.Publisher.Contains(filter, StringComparison.CurrentCultureIgnoreCase);
    }

    public void Dispose()
    {
        _cancellationTokenSource?.Dispose();
        foreach (var item in Packages)
        {
            item.PropertyChanged -= OnPackagePropertyChanged;
        }
    }
}
