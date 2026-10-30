using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using Atualizador.Windows.Services;

namespace Atualizador.Windows.ViewModels
{
    public sealed class UpdateCheckViewModel : INotifyPropertyChanged
    {
        private readonly IGitHubReleaseService _releaseService;
        public event PropertyChangedEventHandler? PropertyChanged;

        private string _status = "Verificando atualizações...";
        public string Status
        {
            get => _status;
            private set { _status = value; OnPropertyChanged(nameof(Status)); }
        }

        public UpdateCheckViewModel()
        {
            _releaseService = new GitHubReleaseService();
            _ = CheckAsync(); // fire‑and‑forget – UI will bind to Status
        }

        private async Task CheckAsync()
        {
            try
            {
                var currentVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";
                const string owner = "lucasmota-dev";
                const string repo = "updater";
                const string asset = "Atualizador-Setup.exe"; // exe installer

                var release = await _releaseService.GetLatestReleaseAsync(owner, repo, asset);
                if (release is null)
                {
                    Status = "Não foi possível obter informações de atualização.";
                    return;
                }

                var latestTag = release.Tag.TrimStart('v', 'V');
                if (new Version(latestTag) <= new Version(currentVersion))
                {
                    Status = "Aplicativo já está na versão mais recente.";
                    // fecha a janela de splash – a aplicação continuará
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        foreach (Window w in System.Windows.Application.Current.Windows)
                            if (w.GetType().Name == "UpdateCheckWindow") w.Close();
                    });
                    return;
                }

                Status = $"Nova versão {release.Tag} encontrada. Baixando…";
                var tempPath = Path.Combine(Path.GetTempPath(), $"Atualizador-{Guid.NewGuid()}.exe");
                using var http = new HttpClient();
                http.DefaultRequestHeaders.Add("User-Agent", "Atualizador.Windows");
                var data = await http.GetByteArrayAsync(release.AssetUrl);
                await File.WriteAllBytesAsync(tempPath, data);

                // Verifica SHA‑256 se informado
                if (!string.IsNullOrWhiteSpace(release.Sha256))
                {
                    using var sha = System.Security.Cryptography.SHA256.Create();
                    var hash = sha.ComputeHash(data);
                    var hashHex = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                    if (!hashHex.Equals(release.Sha256, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Checksum SHA‑256 não confere.");
                }

                // Executa instalador silencioso
                var psi = new ProcessStartInfo
                {
                    FileName = tempPath,
                    Arguments = "/VERYSILENT /NORESTART",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                var proc = Process.Start(psi);
                await proc!.WaitForExitAsync();

                Status = "Atualização concluída. Reiniciando…";
                // fecha o splash e encerra a aplicação – o instalador já substituiu os arquivos
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        foreach (Window w in System.Windows.Application.Current.Windows)
                            if (w.GetType().Name == "UpdateCheckWindow") w.Close();
                        System.Windows.Application.Current.Shutdown();
                    });
            }
            catch (Exception ex)
            {
                Status = $"Falha na atualização: {ex.Message}";
                // encerra splash e permite iniciar aplicação normalmente
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (Window w in System.Windows.Application.Current.Windows)
                        if (w.GetType().Name == "UpdateCheckWindow") w.Close();
                });
            }
        }

        private void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

