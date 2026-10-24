using System.Collections.ObjectModel;
using Atualizador.Windows.Models;
using Atualizador.Windows.Services;

namespace Atualizador.Windows.ViewModels;

public sealed class ExcludedPackagesSettingsViewModel : ViewModelBase
{
    private readonly ExcludedPackagesStore _store;
    private readonly IUserConfirmation _confirmation;

    public ExcludedPackagesSettingsViewModel(
        ExcludedPackagesStore store,
        IUserConfirmation? confirmation = null)
    {
        _store = store;
        _confirmation = confirmation ?? new MessageBoxUserConfirmation();
        ExcludedPackages = new ObservableCollection<ExcludedPackage>(_store.Load());
        _store.Changed += OnStoreChanged;
        RemoveCommand = new DelegateCommand<ExcludedPackage>(RemoveExcludedPackage);
    }

    public ObservableCollection<ExcludedPackage> ExcludedPackages { get; }

    public bool HasExcludedPackages => ExcludedPackages.Count > 0;

    public DelegateCommand<ExcludedPackage> RemoveCommand { get; }

    private void RemoveExcludedPackage(ExcludedPackage? package)
    {
        if (package is null)
        {
            return;
        }

        if (!_confirmation.Confirm(
                "Remover exclusão",
                $"Permitir que '{package.Name}' volte a receber atualizações nas próximas verificações?"))
        {
            return;
        }

        _store.Remove(package);
    }

    private void OnStoreChanged(object? sender, EventArgs e)
    {
        ExcludedPackages.Clear();
        foreach (var item in _store.Load())
        {
            ExcludedPackages.Add(item);
        }

        OnPropertyChanged(nameof(HasExcludedPackages));
    }
}

