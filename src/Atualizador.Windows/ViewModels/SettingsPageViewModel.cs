using Atualizador.Windows.Services;

namespace Atualizador.Windows.ViewModels;

/// <summary>
/// Página Configurações. Só agrupa os ViewModels de cada seção; a lógica fica em cada um deles.
/// </summary>
public sealed class SettingsPageViewModel(
    AppearanceService appearanceService,
    ScheduleSettingsViewModel schedule,
    StartupSettingsViewModel startup,
    TraySettingsViewModel tray,
    ExcludedPackagesSettingsViewModel excludedPackages,
    AppUpdateViewModel appUpdate)
    : PageViewModel("Configurações", "Aparência, agendamento, inicialização e aplicativos excluídos.", "")
{
    public AppearanceSettingsViewModel Appearance { get; } = new(appearanceService);

    public ScheduleSettingsViewModel Schedule { get; } = schedule;

    public StartupSettingsViewModel Startup { get; } = startup;

    public TraySettingsViewModel Tray { get; } = tray;

    public ExcludedPackagesSettingsViewModel ExcludedPackages { get; } = excludedPackages;

    /// <summary>Seção "Sobre o App Updater", com a versão atual e o botão de busca manual.</summary>
    public AppUpdateViewModel AppUpdate { get; } = appUpdate;
}
