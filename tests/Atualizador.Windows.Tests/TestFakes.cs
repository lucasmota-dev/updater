using Atualizador.Windows.Services;

namespace Atualizador.Windows.Tests;

internal sealed class FakeLocator(string? path) : IExecutableLocator
{
    public string? Locate() => path;
}

internal sealed class FakeProcessExecutor(IReadOnlyList<ProcessExecutionResult> results) : IProcessExecutor
{
    private readonly Queue<ProcessExecutionResult> _results = new(results);

    public List<ProcessExecutionRequest> Requests { get; } = [];

    public Task<ProcessExecutionResult> ExecuteAsync(
        ProcessExecutionRequest request,
        CancellationToken cancellationToken = default)
    {
        Requests.Add(request);
        return Task.FromResult(_results.Dequeue());
    }
}

