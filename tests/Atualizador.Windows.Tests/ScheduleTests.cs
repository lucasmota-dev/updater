using Atualizador.Windows.Models;
using Atualizador.Windows.Services;

namespace Atualizador.Windows.Tests;

[TestClass]
public sealed class ScheduleTests
{
    [TestMethod]
    public void ScheduleStore_PreservesVersionedPreferences()
    {
        using var fixture = new TempDirectory();
        var store = new ScheduleStore(Path.Combine(fixture.Path, "schedule.json"));
        var preferences = SchedulePreferences.Defaults with
        {
            Mode = ScheduleMode.ACadaHora,
            IntervalHours = 6,
            Scope = ScheduleScope.TodosElegiveis
        };

        store.Save(preferences);

        var loaded = store.Load();
        Assert.AreEqual(ScheduleMode.ACadaHora, loaded.Mode);
        Assert.AreEqual(6, loaded.IntervalHours);
        Assert.AreEqual(SchedulePreferences.CurrentVersion, loaded.Version);
    }

    [TestMethod]
    public async Task TaskSchedulerService_BuildsFixedSafeAction()
    {
        var executor = new FakeProcessExecutor();
        var service = new TaskSchedulerService(executor, @"C:\Program Files\Atualizador\Atualizador.Windows.exe");
        var preferences = SchedulePreferences.Defaults with
        {
            Mode = ScheduleMode.ACadaHora,
            IntervalHours = 4
        };

        var result = await service.CreateOrUpdateAsync(preferences);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("schtasks.exe", executor.Request.FileName);
        Assert.IsTrue(executor.Request.Arguments.Contains("/Create"));
        Assert.IsTrue(executor.Request.Arguments.Contains("/TN"));
        Assert.IsTrue(executor.Request.Arguments.Contains(TaskSchedulerService.TaskName));
        var arguments = executor.Request.Arguments.ToList();
        var action = arguments[arguments.IndexOf("/TR") + 1];
        StringAssert.Contains(action, "--scheduled");
        Assert.IsFalse(action.Contains("cmd.exe", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public async Task TaskSchedulerService_DisableAndRemoveUseFixedTask()
    {
        var executor = new FakeProcessExecutor();
        var service = new TaskSchedulerService(executor, "Atualizador.Windows.exe");

        await service.DisableAsync();
        Assert.IsTrue(executor.Request.Arguments.Contains("/DISABLE"));
        await service.RemoveAsync();
        Assert.IsTrue(executor.Request.Arguments.Contains("/Delete"));
        Assert.IsTrue(executor.Request.Arguments.Contains(TaskSchedulerService.TaskName));
    }

    [TestMethod]
    public async Task StartupTaskService_UsesFixedLogonAction()
    {
        var executor = new FakeProcessExecutor();
        var service = new StartupTaskService(executor, @"C:\Apps\Atualizador.Windows.exe");

        var result = await service.SetEnabledAsync(true);

        Assert.IsTrue(result.Succeeded);
        Assert.IsTrue(executor.Request.Arguments.Contains("/Create"));
        Assert.IsTrue(executor.Request.Arguments.Contains(StartupTaskService.TaskName));
        Assert.IsTrue(executor.Request.Arguments.Contains("ONLOGON"));
    }

    private sealed class FakeProcessExecutor : IProcessExecutor
    {
        public ProcessExecutionRequest Request { get; private set; } = null!;

        public Task<ProcessExecutionResult> ExecuteAsync(
            ProcessExecutionRequest request,
            CancellationToken cancellationToken = default)
        {
            Request = request;
            return Task.FromResult(new ProcessExecutionResult(true, 0, "ok", string.Empty, false, false, null));
        }
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"schedule-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
