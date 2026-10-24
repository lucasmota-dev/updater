using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Data;
using Atualizador.Windows.Models;
using Atualizador.Windows.Services;

namespace Atualizador.Windows.ViewModels;

public sealed class HistoryPageViewModel : PageViewModel
{
    private readonly HistoryStore _historyStore;
    private readonly ListCollectionView _filteredEntries;
    private HistoryDateFilter _selectedDateFilter;
    private HistoryStatusFilter _selectedStatusFilter;
    private HistoryEntry? _selectedEntry;

    public HistoryPageViewModel(HistoryStore historyStore)
        : base("Histórico", "Acompanhe as verificações e atualizações realizadas neste computador.", "≡")
    {
        _historyStore = historyStore;
        Entries = [];
        _filteredEntries = new ListCollectionView(Entries);
        _filteredEntries.Filter = IsVisibleByFilter;
        RefreshCommand = new DelegateCommand(Load);
        ClearCommand = new DelegateCommand(ClearHistory, () => Entries.Count > 0);
        OpenFolderCommand = new DelegateCommand(OpenFolder);
        Load();
    }

    public ObservableCollection<HistoryEntry> Entries { get; }

    public ICollectionView FilteredEntries => _filteredEntries;

    public IReadOnlyList<HistoryDateFilter> AvailableDateFilters { get; } =
        Enum.GetValues<HistoryDateFilter>();

    public IReadOnlyList<HistoryStatusFilter> AvailableStatusFilters { get; } =
        Enum.GetValues<HistoryStatusFilter>();

    public DelegateCommand RefreshCommand { get; }

    public DelegateCommand ClearCommand { get; }

    public DelegateCommand OpenFolderCommand { get; }

    public HistoryDateFilter SelectedDateFilter
    {
        get => _selectedDateFilter;
        set
        {
            if (SetProperty(ref _selectedDateFilter, value))
            {
                _filteredEntries.Refresh();
                OnPropertyChanged(nameof(IsEmpty));
            }
        }
    }

    public HistoryStatusFilter SelectedStatusFilter
    {
        get => _selectedStatusFilter;
        set
        {
            if (SetProperty(ref _selectedStatusFilter, value))
            {
                _filteredEntries.Refresh();
                OnPropertyChanged(nameof(IsEmpty));
            }
        }
    }

    public HistoryEntry? SelectedEntry
    {
        get => _selectedEntry;
        set => SetProperty(ref _selectedEntry, value);
    }

    public bool IsEmpty => !_filteredEntries.Cast<HistoryEntry>().Any();

    private void Load()
    {
        Entries.Clear();
        foreach (var entry in _historyStore.Load())
        {
            Entries.Add(entry);
        }

        _filteredEntries.Refresh();
        SelectedEntry = _filteredEntries.Cast<HistoryEntry>().FirstOrDefault();
        OnPropertyChanged(nameof(IsEmpty));
        ClearCommand.RaiseCanExecuteChanged();
    }

    private void ClearHistory()
    {
        if (!new MessageBoxUserConfirmation().Confirm(
                "Limpar histórico",
                "Isso removerá todos os registros locais de verificações e atualizações. Deseja continuar?"))
        {
            return;
        }

        _historyStore.Clear();
        Load();
    }

    private void OpenFolder()
    {
        var directory = Path.GetDirectoryName(_historyStore.FilePath);
        if (directory is null)
        {
            return;
        }

        Directory.CreateDirectory(directory);
        using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "explorer.exe",
            UseShellExecute = false,
            CreateNoWindow = true,
            ArgumentList = { directory }
        });
    }

    private bool IsVisibleByFilter(object item)
    {
        if (item is not HistoryEntry entry)
        {
            return false;
        }

        var matchesStatus = SelectedStatusFilter switch
        {
            HistoryStatusFilter.Todos => true,
            _ => entry.Status.ToString().Equals(SelectedStatusFilter.ToString(), StringComparison.Ordinal)
        };
        var matchesDate = SelectedDateFilter switch
        {
            HistoryDateFilter.Todos => true,
            HistoryDateFilter.UltimosSeteDias => entry.FinishedAt >= DateTimeOffset.Now.AddDays(-7),
            HistoryDateFilter.UltimosTrintaDias => entry.FinishedAt >= DateTimeOffset.Now.AddDays(-30),
            _ => true
        };
        return matchesStatus && matchesDate;
    }
}
