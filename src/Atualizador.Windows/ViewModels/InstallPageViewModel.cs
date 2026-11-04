using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using Atualizador.Windows.Models;
using Atualizador.Windows.Services;

namespace Atualizador.Windows.ViewModels;

/// <summary>Uma linha da lista de resultados da pesquisa: o pacote, a seleção e se já está na fila.</summary>
public sealed class InstallItemViewModel(InstallablePackage package) : ViewModelBase
{
    private bool _isSelected;
    private string _statusMessage = string.Empty;

    public InstallablePackage Package { get; } = package;

    public string Name => Package.Name;

    public string Id => Package.Id;

    public string Version => Package.Version;

    public string Source => Package.Source;

    public string Publisher => Package.Publisher;

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    /// <summary>Situação do pacote na fila ("Na fila", "Instalado"…), copiada do item da fila.</summary>
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }
}

/// <summary>
/// Página Instalar: pesquisa no catálogo do WinGet e uma <b>fila de instalação</b>.
/// <para>
/// A pesquisa e a fila são independentes: dá para pesquisar outros aplicativos e adicioná-los à fila
/// enquanto ela instala. A fila instala um aplicativo por vez, na ordem em que foram adicionados,
/// e divide a <see cref="PackageOperationGate"/> com a página Atualizações, para o WinGet nunca rodar
/// duas instalações ao mesmo tempo. Cada instalação vira um registro no histórico.
/// </para>
/// </summary>
public sealed class InstallPageViewModel : PageViewModel, IDisposable
{
    private readonly IPackageUpdateService _wingetService;
    private readonly HistoryStore _historyStore;
    private readonly IUserConfirmation _confirmation;
    private readonly NotificationService? _notificationService;
    private readonly PackageOperationGate _operationGate;
    private readonly ListCollectionView _filteredPackages;
    private readonly CancellationTokenSource _lifetime = new();
    private CancellationTokenSource? _searchCancellation;
    private CancellationTokenSource? _currentInstallCancellation;
    private string _searchQuery = string.Empty;
    private string _filterText = string.Empty;
    private string _statusMessage = "Digite o nome ou ID de um aplicativo e clique em Pesquisar.";
    private bool _isSearching;
    private bool _isInstalling;
    private bool _isError;

    public InstallPageViewModel(
        IPackageUpdateService wingetService,
        HistoryStore? historyStore = null,
        IUserConfirmation? confirmation = null,
        NotificationService? notificationService = null,
        PackageOperationGate? operationGate = null)
        : base("Instalar", "Pesquise aplicativos do catálogo do WinGet e adicione-os à fila de instalação.", "")
    {
        _wingetService = wingetService;
        _historyStore = historyStore ?? new HistoryStore();
        _confirmation = confirmation ?? new MessageBoxUserConfirmation();
        _notificationService = notificationService;
        _operationGate = operationGate ?? new PackageOperationGate();
        _filteredPackages = new ListCollectionView(Packages) { Filter = FilterPackageItem };

        SearchCommand = new AsyncDelegateCommand(SearchAsync, () => !string.IsNullOrWhiteSpace(SearchQuery) && !IsSearching);
        AddSelectedToQueueCommand = new DelegateCommand(AddSelectedToQueue, () => HasSelectedPackages);
        CancelCurrentCommand = new DelegateCommand(CancelCurrent, () => IsInstalling);
        RemoveFromQueueCommand = new DelegateCommand<InstallQueueItemViewModel>(RemoveFromQueue, item => item?.IsPending == true);
        ClearFinishedCommand = new DelegateCommand(ClearFinished, () => Queue.Any(item => item.IsFinished));
        Queue.CollectionChanged += (_, _) => RaiseQueueChanged();
    }

    /// <summary>Resultados da última pesquisa.</summary>
    public ObservableCollection<InstallItemViewModel> Packages { get; } = [];

    public ICollectionView FilteredPackages => _filteredPackages;

    /// <summary>Fila de instalação, na ordem em que os aplicativos serão instalados.</summary>
    public ObservableCollection<InstallQueueItemViewModel> Queue { get; } = [];

    public AsyncDelegateCommand SearchCommand { get; }

    /// <summary>Adiciona os resultados marcados ao fim da fila e começa a instalar, se ela estiver parada.</summary>
    public DelegateCommand AddSelectedToQueueCommand { get; }

    /// <summary>Cancela só a instalação em andamento; a fila continua com o próximo.</summary>
    public DelegateCommand CancelCurrentCommand { get; }

    /// <summary>Tira da fila um aplicativo que ainda não começou a instalar.</summary>
    public DelegateCommand<InstallQueueItemViewModel> RemoveFromQueueCommand { get; }

    /// <summary>Remove da lista os itens já concluídos, com falha ou cancelados.</summary>
    public DelegateCommand ClearFinishedCommand { get; }

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
                OnPropertyChanged(nameof(AreAllSelected));
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public bool IsSearching
    {
        get => _isSearching;
        private set
        {
            if (SetProperty(ref _isSearching, value))
            {
                OnPropertyChanged(nameof(IsBusy));
                SearchCommand.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>A fila está instalando. Também impede o app de se atualizar no meio da instalação.</summary>
    public bool IsInstalling
    {
        get => _isInstalling;
        private set
        {
            if (SetProperty(ref _isInstalling, value))
            {
                CancelCurrentCommand.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>Pesquisa em andamento: bloqueia a seleção dos resultados (a fila não bloqueia nada).</summary>
    public bool IsBusy => IsSearching;

    public bool IsError
    {
        get => _isError;
        private set => SetProperty(ref _isError, value);
    }

    public int SelectedCount => Packages.Count(package => package.IsSelected);

    public bool HasSelectedPackages => SelectedCount > 0;

    public bool HasPackages => _filteredPackages.Cast<object>().Any();

    public bool HasQueue => Queue.Count > 0;

    /// <summary>Resumo da fila para o cabeçalho, ex.: "2 de 5 concluídos • instalando Firefox".</summary>
    public string QueueSummary
    {
        get
        {
            var finished = Queue.Count(item => item.IsFinished);
            var running = Queue.FirstOrDefault(item => item.IsRunning);
            var summary = $"{finished} de {Queue.Count} concluído(s)";
            return running is null ? summary : $"{summary} • instalando {running.Name}";
        }
    }

    /// <summary>Progresso geral da fila (0 a 100).</summary>
    public double QueueProgress => Queue.Count == 0 ? 0 : (double)Queue.Count(item => item.IsFinished) / Queue.Count * 100;

    /// <summary>
    /// Caixa "marcar/desmarcar todos" do cabeçalho: marcada se todos os resultados visíveis estão
    /// marcados, desmarcada se nenhum, indeterminada (null) se só alguns.
    /// </summary>
    public bool? AreAllSelected
    {
        get
        {
            var visible = _filteredPackages.Cast<InstallItemViewModel>().ToArray();
            var selected = visible.Count(package => package.IsSelected);
            return selected == 0 ? false : selected == visible.Length ? true : null;
        }
        set
        {
            var select = value == true;
            foreach (var package in _filteredPackages.Cast<InstallItemViewModel>())
            {
                package.IsSelected = select;
            }

            RaiseSelectionChanged();
        }
    }

    /// <summary>
    /// Pesquisa o termo digitado (somente na fonte <c>winget</c>) e substitui os resultados.
    /// Pode rodar enquanto a fila instala: a fila não depende da lista de resultados.
    /// </summary>
    public async Task SearchAsync()
    {
        if (IsSearching || string.IsNullOrWhiteSpace(SearchQuery))
        {
            return;
        }

        IsSearching = true;
        IsError = false;
        StatusMessage = $"Pesquisando por '{SearchQuery.Trim()}' no catálogo do WinGet…";
        _searchCancellation?.Dispose();
        _searchCancellation = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);

        try
        {
            var result = await _wingetService.SearchAsync(SearchQuery.Trim(), "winget", _searchCancellation.Token);
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

    /// <summary>
    /// Pede confirmação e põe os marcados no fim da fila. Pacotes que já estão na fila (esperando ou
    /// instalando) são ignorados. Se a fila estava parada, começa a instalar.
    /// </summary>
    private void AddSelectedToQueue()
    {
        var selected = Packages.Where(package => package.IsSelected).ToArray();
        var queuedKeys = Queue.Where(item => !item.IsFinished).Select(item => item.Key).ToHashSet();
        var toAdd = selected.Where(package => !queuedKeys.Contains(InstallQueueItemViewModel.QueueKey(package.Package))).ToArray();
        if (toAdd.Length == 0)
        {
            StatusMessage = "Os aplicativos marcados já estão na fila.";
            return;
        }

        var names = string.Join(", ", toAdd.Select(package => package.Name));
        if (!_confirmation.Confirm(
                "Adicionar à fila de instalação",
                $"Adicionar {toAdd.Length} aplicativo(s) à fila? Eles serão instalados um de cada vez.\n\n{names}\n\n"
                + "Atenção: estar no catálogo do WinGet não garante que o aplicativo seja seguro. Revise o publicador."))
        {
            return;
        }

        foreach (var package in toAdd)
        {
            var item = new InstallQueueItemViewModel(package.Package);
            item.PropertyChanged += OnQueueItemChanged;
            Queue.Add(item);
            package.IsSelected = false;
        }

        SyncResultStatuses();
        StatusMessage = $"{toAdd.Length} aplicativo(s) adicionado(s) à fila de instalação.";
        _ = ProcessQueueAsync();
    }

    /// <summary>
    /// Instala os itens pendentes, um por vez, até a fila esvaziar. Só uma execução roda por vez:
    /// itens adicionados durante a instalação entram no mesmo ciclo.
    /// </summary>
    private async Task ProcessQueueAsync()
    {
        if (IsInstalling)
        {
            return;
        }

        IsInstalling = true;
        var processed = new List<InstallQueueItemViewModel>();
        try
        {
            while (!_lifetime.IsCancellationRequested && Queue.FirstOrDefault(item => item.IsPending) is { } item)
            {
                processed.Add(item);
                await InstallOneAsync(item);
            }
        }
        finally
        {
            IsInstalling = false;
        }

        if (processed.Count > 0 && !_lifetime.IsCancellationRequested)
        {
            var succeeded = processed.Count(item => item.Status == PackageUpdateStatus.Succeeded);
            var message = $"Fila concluída: {succeeded} de {processed.Count} aplicativo(s) instalado(s).";
            StatusMessage = message;
            _notificationService?.Publish(new(
                succeeded == processed.Count ? NotificationKind.Sucesso : NotificationKind.Informacao,
                "Instalação finalizada",
                message));
        }
    }

    private async Task InstallOneAsync(InstallQueueItemViewModel item)
    {
        var startedAt = DateTimeOffset.Now;
        _currentInstallCancellation = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        try
        {
            item.Status = PackageUpdateStatus.InProgress;
            if (_operationGate.IsBusy)
            {
                item.StatusMessage = "Aguardando outra atualização terminar…";
            }

            using (await _operationGate.EnterAsync(_currentInstallCancellation.Token))
            {
                item.StatusMessage = "Instalando…";
                var result = await _wingetService.InstallAsync(item.Package, _currentInstallCancellation.Token);

                // Instalador que exige administrador: oferece repetir só este pacote com o aviso do UAC.
                if (result.Status == PackageUpdateStatus.RequiresElevation && ConfirmElevation(item.Name))
                {
                    item.StatusMessage = "Aguardando a permissão de administrador…";
                    result = await _wingetService.InstallElevatedAsync(item.Package, _currentInstallCancellation.Token);
                }

                item.Status = result.Status;
                item.StatusMessage = result.Message;
            }
        }
        catch (OperationCanceledException)
        {
            item.Status = PackageUpdateStatus.Canceled;
            item.StatusMessage = "Instalação cancelada.";
        }
        finally
        {
            _currentInstallCancellation.Dispose();
            _currentInstallCancellation = null;
        }

        _historyStore.Append(new HistoryEntry(
            Guid.NewGuid(),
            startedAt,
            DateTimeOffset.Now,
            HistoryOperation.Instalacao,
            "winget",
            1,
            item.Status switch
            {
                PackageUpdateStatus.Succeeded => HistoryEntryStatus.Sucesso,
                PackageUpdateStatus.Canceled => HistoryEntryStatus.Cancelado,
                _ => HistoryEntryStatus.Falha
            },
            $"{item.Name}: {item.StatusMessage}",
            [new HistoryPackageResult(item.Name, item.Id, item.Package.Source, item.Status, item.StatusMessage)]));
    }

    private bool ConfirmElevation(string name) => _confirmation.Confirm(
        "Permissão de administrador",
        $"O instalador de {name} exige permissão de administrador.\n\n"
        + "Tentar de novo como administrador? O Windows vai mostrar o aviso de permissão (UAC) só para este aplicativo.");

    private void CancelCurrent()
    {
        if (_currentInstallCancellation is { IsCancellationRequested: false } cancellation)
        {
            StatusMessage = "Cancelando a instalação em andamento… O instalador externo pode não reverter alterações.";
            cancellation.Cancel();
        }
    }

    private void RemoveFromQueue(InstallQueueItemViewModel? item)
    {
        if (item is { IsPending: true })
        {
            item.PropertyChanged -= OnQueueItemChanged;
            Queue.Remove(item);
            SyncResultStatuses();
        }
    }

    private void ClearFinished()
    {
        foreach (var item in Queue.Where(item => item.IsFinished).ToArray())
        {
            item.PropertyChanged -= OnQueueItemChanged;
            Queue.Remove(item);
        }

        SyncResultStatuses();
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
        SyncResultStatuses();
        RaiseSelectionChanged();
        OnPropertyChanged(nameof(HasPackages));
    }

    /// <summary>Mostra nos resultados da pesquisa a situação de cada pacote que está na fila.</summary>
    private void SyncResultStatuses()
    {
        var byKey = Queue.GroupBy(item => item.Key).ToDictionary(group => group.Key, group => group.Last());
        foreach (var package in Packages)
        {
            package.StatusMessage = byKey.TryGetValue(InstallQueueItemViewModel.QueueKey(package.Package), out var queued)
                ? queued.StatusMessage
                : string.Empty;
        }
    }

    private void OnPackagePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(InstallItemViewModel.IsSelected))
        {
            RaiseSelectionChanged();
        }
    }

    private void OnQueueItemChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(InstallQueueItemViewModel.Status) or nameof(InstallQueueItemViewModel.StatusMessage))
        {
            SyncResultStatuses();
            RaiseQueueChanged();
        }
    }

    private void RaiseSelectionChanged()
    {
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(HasSelectedPackages));
        OnPropertyChanged(nameof(AreAllSelected));
        AddSelectedToQueueCommand.RaiseCanExecuteChanged();
    }

    private void RaiseQueueChanged()
    {
        OnPropertyChanged(nameof(HasQueue));
        OnPropertyChanged(nameof(QueueSummary));
        OnPropertyChanged(nameof(QueueProgress));
        ClearFinishedCommand.RaiseCanExecuteChanged();
        RemoveFromQueueCommand.RaiseCanExecuteChanged();
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
        _lifetime.Cancel();
        _searchCancellation?.Dispose();
        foreach (var item in Packages)
        {
            item.PropertyChanged -= OnPackagePropertyChanged;
        }

        foreach (var item in Queue)
        {
            item.PropertyChanged -= OnQueueItemChanged;
        }
    }
}
