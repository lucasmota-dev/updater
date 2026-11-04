using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text.Json;
using System.Text.Json.Serialization;
using Atualizador.Windows.Models;

namespace Atualizador.Windows.Services;

/// <summary>
/// Instala as atualizações do Windows sem elevar o aplicativo inteiro.
/// <para>
/// A janela principal roda sem permissão de administrador. Para instalar, ela abre uma segunda
/// cópia do próprio executável com <c>runas</c> (o Windows mostra o aviso do UAC) e um argumento
/// fixo: <c>--install-windows-updates &lt;id&gt;</c>. Essa cópia elevada não abre janela, chama
/// <see cref="WindowsUpdateService.InstallAll"/> e envia o progresso em JSON, uma linha por etapa,
/// por um <i>named pipe</i> cujo nome inclui o id.
/// </para>
/// <para>
/// O pipe foi escolhido em vez de um arquivo: um processo elevado gravando em um caminho que um
/// processo comum consegue manipular (atalhos, junções) é uma falha de segurança conhecida.
/// O processo elevado não recebe nenhum outro dado da janela: só o id, validado como GUID.
/// </para>
/// </summary>
public static class WindowsUpdateInstallLauncher
{
    public const string ElevatedArgument = "--install-windows-updates";

    private const string PipePrefix = "AppUpdater.WindowsUpdate.";
    private const string MutexName = "Atualizador.Windows.WindowsUpdateInstall.v1";
    private const int UserCanceledUacError = 1223;

    // Uma mensagem por linha: o JSON não pode ser indentado.
    private static readonly JsonSerializerOptions PipeJsonOptions = new() { Converters = { new JsonStringEnumConverter() } };

    /// <summary>Reconhece a linha de comando do processo elevado: exatamente o argumento e um GUID.</summary>
    public static bool TryParseElevatedArguments(IReadOnlyList<string> args, out Guid runId)
    {
        runId = Guid.Empty;
        return args.Count == 2
            && args[0].Equals(ElevatedArgument, StringComparison.OrdinalIgnoreCase)
            && Guid.TryParseExact(args[1], "N", out runId);
    }

    /// <summary>
    /// Janela principal: abre o processo elevado, repassa cada etapa a <paramref name="progress"/>
    /// e devolve o resultado final. Se o usuário recusar o UAC, devolve um resultado de cancelamento.
    /// </summary>
    public static async Task<WindowsUpdateProgress> InstallElevatedAsync(
        IProgress<WindowsUpdateProgress> progress,
        CancellationToken cancellationToken = default)
    {
        var executable = Environment.ProcessPath;
        if (string.IsNullOrEmpty(executable))
        {
            return Failure("Não foi possível localizar o executável do aplicativo.");
        }

        var runId = Guid.NewGuid();
        using var pipe = new NamedPipeServerStream(
            PipePrefix + runId.ToString("N"), PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

        Process? process;
        try
        {
            process = Process.Start(new ProcessStartInfo(executable)
            {
                UseShellExecute = true, // obrigatório para "runas" (pedido de elevação)
                Verb = "runas",
                Arguments = $"{ElevatedArgument} {runId:N}"
            });
        }
        catch (Win32Exception exception) when (exception.NativeErrorCode == UserCanceledUacError)
        {
            return new(WindowsUpdateStage.Finished, 0, 0, "A instalação foi cancelada: a permissão de administrador não foi concedida.", false);
        }

        if (process is null)
        {
            return Failure("Não foi possível iniciar a instalação.");
        }

        using (process)
        {
            WindowsUpdateProgress? last = null;
            var exitTask = process.WaitForExitAsync(cancellationToken);
            using var connectionCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var connectTask = pipe.WaitForConnectionAsync(connectionCancellation.Token);

            if (await Task.WhenAny(connectTask, exitTask) == connectTask && connectTask.IsCompletedSuccessfully)
            {
                using var reader = new StreamReader(pipe);
                while (await reader.ReadLineAsync(cancellationToken) is { } line)
                {
                    if (TryDeserialize(line) is { } message)
                    {
                        last = message;
                        progress.Report(message);
                    }
                }
            }
            else
            {
                // O processo terminou sem conectar (ex.: falha ao abrir). Encerra a espera do pipe.
                await connectionCancellation.CancelAsync();
            }

            await exitTask;
            return last is { Stage: WindowsUpdateStage.Finished }
                ? last
                : process.ExitCode == 0
                    ? new(WindowsUpdateStage.Finished, 0, 0, "Instalação concluída.", true)
                    : Failure($"A instalação terminou com o código {process.ExitCode}.");
        }
    }

    /// <summary>
    /// Processo elevado: instala tudo e envia o progresso pelo pipe. Retorna o código de saída
    /// (0 = sucesso, 1 = falha, 2 = outra instalação já em andamento).
    /// </summary>
    public static int RunElevated(Guid runId)
    {
        using var mutex = new Mutex(false, MutexName, out var createdNew);
        if (!createdNew || !mutex.WaitOne(TimeSpan.Zero))
        {
            return 2;
        }

        using var pipe = new NamedPipeClientStream(".", PipePrefix + runId.ToString("N"), PipeDirection.Out);
        StreamWriter? writer = null;
        try
        {
            pipe.Connect(TimeSpan.FromSeconds(10));
            writer = new StreamWriter(pipe) { AutoFlush = true };
        }
        catch (Exception exception) when (exception is TimeoutException or IOException)
        {
            // Sem janela para receber o progresso: instala mesmo assim e informa só pelo código de saída.
        }

        var final = WindowsUpdateService.InstallAll(message =>
        {
            try
            {
                writer?.WriteLine(JsonSerializer.Serialize(message, PipeJsonOptions));
            }
            catch (IOException)
            {
                writer = null; // A janela foi fechada; a instalação continua.
            }
        });

        writer?.Dispose();
        return final.Succeeded == true ? 0 : 1;
    }

    private static WindowsUpdateProgress? TryDeserialize(string line)
    {
        try
        {
            return JsonSerializer.Deserialize<WindowsUpdateProgress>(line, PipeJsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static WindowsUpdateProgress Failure(string message) =>
        new(WindowsUpdateStage.Finished, 0, 0, message, false);
}
