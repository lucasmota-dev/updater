using Atualizador.Windows.Models;

namespace Atualizador.Windows.ViewModels;

/// <summary>Um aplicativo na fila de instalação: o pacote e o estado da instalação dele.</summary>
public sealed class InstallQueueItemViewModel(InstallablePackage package, PackageIconViewModel? icon = null) : ViewModelBase
{
    private PackageUpdateStatus _status = PackageUpdateStatus.NotStarted;
    private string _statusMessage = "Na fila";

    public InstallablePackage Package { get; } = package;

    /// <summary>O mesmo ícone do resultado da pesquisa, para não buscá-lo de novo.</summary>
    public PackageIconViewModel Icon { get; } = icon ?? new(package.Name, package.Id, null);

    public string Name => Package.Name;

    public string Id => Package.Id;

    public string Version => Package.Version;

    /// <summary>Chave de comparação (ID + origem), para não pôr o mesmo pacote duas vezes na fila.</summary>
    public string Key => QueueKey(Package);

    public PackageUpdateStatus Status
    {
        get => _status;
        set
        {
            if (SetProperty(ref _status, value))
            {
                OnPropertyChanged(nameof(IsPending));
                OnPropertyChanged(nameof(IsRunning));
                OnPropertyChanged(nameof(IsFinished));
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>Ainda não começou: pode ser removido da fila.</summary>
    public bool IsPending => Status == PackageUpdateStatus.NotStarted;

    public bool IsRunning => Status == PackageUpdateStatus.InProgress;

    public bool IsFinished => !IsPending && !IsRunning;

    public static string QueueKey(InstallablePackage package) => $"{package.Source}\u001f{package.Id}".ToUpperInvariant();
}
