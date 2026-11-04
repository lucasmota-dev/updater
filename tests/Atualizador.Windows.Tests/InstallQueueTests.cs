using Atualizador.Windows.Models;
using Atualizador.Windows.Services;
using Atualizador.Windows.ViewModels;

namespace Atualizador.Windows.Tests;

[TestClass]
public sealed class InstallQueueTests
{
    private string _directory = string.Empty;

    [TestInitialize]
    public void CreateDirectory()
    {
        _directory = Path.Combine(Path.GetTempPath(), $"atualizador-queue-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_directory);
    }

    [TestCleanup]
    public void DeleteDirectory() => Directory.Delete(_directory, recursive: true);

    [TestMethod]
    public async Task Queue_InstallsOneAtATimeAndAcceptsNewSearchesMeanwhile()
    {
        var service = new ControlledInstallService();
        var viewModel = CreateViewModel(service);

        // Primeira pesquisa: adiciona Firefox à fila; a instalação começa e fica "presa".
        service.SearchResults = [Package("Mozilla.Firefox")];
        viewModel.SearchQuery = "firefox";
        await viewModel.SearchAsync();
        viewModel.AreAllSelected = true;
        viewModel.AddSelectedToQueueCommand.Execute(null);
        await WaitUntilAsync(() => service.Started.Count == 1);

        // Enquanto instala: nova pesquisa e mais um app na fila (era aqui que falhava).
        service.SearchResults = [Package("Discord.Discord")];
        viewModel.SearchQuery = "discord";
        await viewModel.SearchAsync();
        viewModel.AreAllSelected = true;
        viewModel.AddSelectedToQueueCommand.Execute(null);

        Assert.HasCount(2, viewModel.Queue);
        Assert.HasCount(1, service.Started, "O segundo app só começa depois que o primeiro terminar.");

        service.FinishCurrent();
        await WaitUntilAsync(() => service.Started.Count == 2);
        service.FinishCurrent();
        await WaitUntilAsync(() => !viewModel.IsInstalling);

        CollectionAssert.AreEqual(new[] { "Mozilla.Firefox", "Discord.Discord" }, service.Started.ToArray());
        Assert.IsTrue(viewModel.Queue.All(item => item.Status == PackageUpdateStatus.Succeeded));
    }

    [TestMethod]
    public async Task Queue_DoesNotAddTheSamePendingPackageTwice()
    {
        var service = new ControlledInstallService { SearchResults = [Package("Mozilla.Firefox")] };
        var viewModel = CreateViewModel(service);
        viewModel.SearchQuery = "firefox";
        await viewModel.SearchAsync();

        viewModel.AreAllSelected = true;
        viewModel.AddSelectedToQueueCommand.Execute(null);
        viewModel.AreAllSelected = true;
        viewModel.AddSelectedToQueueCommand.Execute(null);

        Assert.HasCount(1, viewModel.Queue);
        service.FinishCurrent();
    }

    [TestMethod]
    public async Task Queue_PendingItemCanBeRemovedBeforeItStarts()
    {
        var service = new ControlledInstallService { SearchResults = [Package("A.One"), Package("B.Two")] };
        var viewModel = CreateViewModel(service);
        viewModel.SearchQuery = "apps";
        await viewModel.SearchAsync();
        viewModel.AreAllSelected = true;
        viewModel.AddSelectedToQueueCommand.Execute(null);
        await WaitUntilAsync(() => service.Started.Count == 1);

        var pending = viewModel.Queue.Single(item => item.IsPending);
        viewModel.RemoveFromQueueCommand.Execute(pending);
        service.FinishCurrent();
        await WaitUntilAsync(() => !viewModel.IsInstalling);

        Assert.HasCount(1, service.Started);
        Assert.HasCount(1, viewModel.Queue);
    }

    private InstallPageViewModel CreateViewModel(ControlledInstallService service) =>
        new(service, new HistoryStore(Path.Combine(_directory, "history.json")), new AlwaysConfirm());

    private static InstallablePackage Package(string id) => new(id, id, "1.0", "winget", id.Split('.')[0]);

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        for (var attempt = 0; attempt < 200 && !condition(); attempt++)
        {
            await Task.Delay(10);
        }

        Assert.IsTrue(condition(), "A condição esperada não foi atingida.");
    }

    private sealed class AlwaysConfirm : IUserConfirmation
    {
        public bool Confirm(string title, string message) => true;
    }

    /// <summary>Serviço falso: cada instalação só termina quando o teste chama <see cref="FinishCurrent"/>.</summary>
    private sealed class ControlledInstallService : IPackageUpdateService
    {
        private TaskCompletionSource? _current;

        public IReadOnlyList<InstallablePackage> SearchResults { get; set; } = [];

        public List<string> Started { get; } = [];

        public void FinishCurrent() => _current?.TrySetResult();

        public Task<WingetSearchResult> SearchAsync(string query, string source = "winget", CancellationToken cancellationToken = default) =>
            Task.FromResult(new WingetSearchResult(WingetSearchStatus.Success, SearchResults, "ok"));

        public async Task<PackageInstallResult> InstallAsync(InstallablePackage package, CancellationToken cancellationToken = default)
        {
            Started.Add(package.Id);
            _current = new TaskCompletionSource();
            await _current.Task;
            return new PackageInstallResult(package, PackageUpdateStatus.Succeeded, "Instalado.");
        }

        public Task<WingetDiscoveryResult> DiscoverUpdatesAsync(bool includeUnknown = false, CancellationToken cancellationToken = default) =>
            Task.FromResult(new WingetDiscoveryResult(WingetDiscoveryStatus.NoUpdates, [], [], "1.0", "ok", 0, []));

        public Task<PackageUpdateResult> UpgradeAsync(UpdatablePackage package, CancellationToken cancellationToken = default) =>
            Task.FromResult(new PackageUpdateResult(package, PackageUpdateStatus.Succeeded, "ok", 0));
    }
}
