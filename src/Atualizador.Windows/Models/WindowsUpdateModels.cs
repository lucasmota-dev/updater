namespace Atualizador.Windows.Models;

/// <summary>Atualização do Windows disponível (lida do Windows Update Agent).</summary>
/// <param name="KbArticle">Número do artigo "KB" da Microsoft, quando existe (ex.: "KB5031455").</param>
/// <param name="RebootMayBeRequired">O Windows avisa que esta atualização pode exigir reinício.</param>
public sealed record WindowsUpdateInfo(
    string Id,
    string Title,
    string? KbArticle,
    string Category,
    long SizeBytes,
    bool RebootMayBeRequired)
{
    /// <summary>Tamanho para exibição ("35 MB"). Zero quando o Windows não informa.</summary>
    public string SizeText => SizeBytes switch
    {
        <= 0 => "—",
        < 1024 * 1024 => $"{SizeBytes / 1024.0:0} KB",
        < 1024L * 1024 * 1024 => $"{SizeBytes / (1024.0 * 1024):0} MB",
        _ => $"{SizeBytes / (1024.0 * 1024 * 1024):0.0} GB"
    };
}

/// <summary>Resultado da procura de atualizações do Windows.</summary>
public sealed record WindowsUpdateSearchResult(
    bool Succeeded,
    IReadOnlyList<WindowsUpdateInfo> Updates,
    string Message);

/// <summary>Etapa da instalação, enviada pelo processo elevado à janela principal.</summary>
public enum WindowsUpdateStage
{
    Searching,
    Downloading,
    Installing,
    Finished
}

/// <summary>Resultado da instalação de uma atualização.</summary>
public sealed record WindowsUpdateItemResult(string Title, string? KbArticle, bool Succeeded, string Message);

/// <summary>
/// Progresso da instalação. O processo elevado envia um destes, em JSON, a cada mudança;
/// <see cref="Succeeded"/> só é preenchido na mensagem final (<see cref="WindowsUpdateStage.Finished"/>).
/// </summary>
public sealed record WindowsUpdateProgress(
    WindowsUpdateStage Stage,
    int Current,
    int Total,
    string Message,
    bool? Succeeded = null,
    bool RebootRequired = false,
    IReadOnlyList<WindowsUpdateItemResult>? Results = null);
