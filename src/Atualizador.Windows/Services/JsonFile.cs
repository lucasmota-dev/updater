using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atualizador.Windows.Services;

/// <summary>
/// Leitura e gravação dos arquivos JSON de preferências e histórico.
/// Todas as classes "Store" usam estes métodos para ter o mesmo comportamento:
/// <list type="bullet">
/// <item>arquivo ausente, corrompido ou inacessível é tratado como "sem dados" (o app nunca trava por isso);</item>
/// <item>a gravação é atômica: escreve em um arquivo temporário e só então substitui o original,
/// para que uma queda de energia não deixe um JSON pela metade.</item>
/// </list>
/// </summary>
internal static class JsonFile
{
    /// <summary>Opções padrão: JSON indentado (legível pelo usuário) e enums gravados como texto.</summary>
    public static JsonSerializerOptions DefaultOptions { get; } = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>Lê e desserializa o arquivo. Retorna <c>null</c> se ele não existir ou não puder ser lido.</summary>
    public static T? TryRead<T>(string path) where T : class
    {
        try
        {
            return File.Exists(path)
                ? JsonSerializer.Deserialize<T>(File.ReadAllText(path), DefaultOptions)
                : null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Serializa e grava o valor de forma atômica. Retorna <c>false</c> se o disco recusar a gravação;
    /// as preferências são "melhor esforço" e uma falha aqui não deve encerrar o aplicativo.
    /// </summary>
    public static bool TryWrite<T>(string path, T value)
    {
        var temporaryPath = $"{path}.{Guid.NewGuid():N}.tmp";
        try
        {
            if (Path.GetDirectoryName(path) is { } directory)
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(value, DefaultOptions));
            File.Move(temporaryPath, path, overwrite: true);
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }
        finally
        {
            TryDelete(temporaryPath);
        }
    }

    /// <summary>Apaga o arquivo se ele existir, ignorando falhas de acesso.</summary>
    public static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Arquivo temporário ou já removido: não há o que fazer.
        }
    }
}
