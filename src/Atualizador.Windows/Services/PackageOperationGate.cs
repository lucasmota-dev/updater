namespace Atualizador.Windows.Services;

/// <summary>
/// Garante que só uma instalação ou atualização de pacote rode por vez no app inteiro.
/// O Windows Installer recusa duas instalações simultâneas (erro 1618) e muitos instaladores
/// falham do mesmo jeito; por isso a fila de instalação e a página Atualizações dividem esta trava.
/// Consultas (pesquisa, lista de atualizações) não usam a trava e podem rodar a qualquer momento.
/// </summary>
public sealed class PackageOperationGate : IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    /// <summary>Há uma instalação ou atualização em andamento agora.</summary>
    public bool IsBusy => _semaphore.CurrentCount == 0;

    /// <summary>Espera a vez e devolve um objeto que libera a trava ao ser descartado (<c>using</c>).</summary>
    public async Task<IDisposable> EnterAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        return new Releaser(_semaphore);
    }

    public void Dispose() => _semaphore.Dispose();

    private sealed class Releaser(SemaphoreSlim semaphore) : IDisposable
    {
        private SemaphoreSlim? _semaphore = semaphore;

        public void Dispose()
        {
            try
            {
                Interlocked.Exchange(ref _semaphore, null)?.Release();
            }
            catch (ObjectDisposedException)
            {
                // O app está fechando e a trava já foi descartada.
            }
        }
    }
}
