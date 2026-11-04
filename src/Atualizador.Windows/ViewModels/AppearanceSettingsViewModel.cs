using Atualizador.Windows.Models;
using Atualizador.Windows.Services;

namespace Atualizador.Windows.ViewModels;

/// <summary>
/// Seção "Aparência" de Configurações: tema (Sistema/Claro/Escuro) e cor de destaque.
/// Cada mudança é aplicada na hora e salva pelo <see cref="AppearanceService"/>.
/// </summary>
public sealed class AppearanceSettingsViewModel : ViewModelBase
{
    // Opções especiais da lista de cores: "do sistema" segue o Windows; "personalizada" usa o campo hexadecimal.
    private static readonly AccentOption SystemAccent = new("Cor do sistema", AppearanceService.SystemAccentKey, IsSystem: true);
    private static readonly AccentOption CustomAccent = new("Personalizada", "", IsCustom: true);

    private readonly AppearanceService _appearanceService;
    private AppearanceMode _selectedMode;
    private AccentOption _selectedAccent;
    private string _customAccentHex;
    private string _validationMessage = string.Empty;

    public AppearanceSettingsViewModel(AppearanceService appearanceService)
    {
        _appearanceService = appearanceService;
        _selectedMode = appearanceService.Preferences.Mode;
        _customAccentHex = appearanceService.Preferences.AccentHex;
        _selectedAccent = FindAccentOption(_customAccentHex);
        ApplyCustomAccentCommand = new DelegateCommand(ApplyCustomAccent);
        RestoreDefaultsCommand = new DelegateCommand(RestoreDefaults);
    }

    public IReadOnlyList<AppearanceMode> AvailableModes { get; } = Enum.GetValues<AppearanceMode>();

    public IReadOnlyList<AccentOption> AccentOptions { get; } =
    [
        SystemAccent,
        new("Azul", "#0067C0"),
        new("Roxo", "#744DA9"),
        new("Verde", "#107C10"),
        new("Laranja", "#CA5010"),
        CustomAccent
    ];

    public AppearanceMode SelectedMode
    {
        get => _selectedMode;
        set
        {
            if (SetProperty(ref _selectedMode, value))
            {
                _appearanceService.SetMode(value);
            }
        }
    }

    /// <summary>
    /// Cor escolhida na lista. "Personalizada" não aplica nada sozinha: espera o botão Aplicar,
    /// porque o texto hexadecimal ainda pode estar incompleto.
    /// </summary>
    public AccentOption SelectedAccent
    {
        get => _selectedAccent;
        set
        {
            if (!SetProperty(ref _selectedAccent, value))
            {
                return;
            }

            if (value.IsSystem)
            {
                CustomAccentHex = string.Empty;
                ClearValidationMessage();
                _appearanceService.SetSystemAccent();
            }
            else if (!value.IsCustom)
            {
                CustomAccentHex = value.Hex;
                ClearValidationMessage();
                _appearanceService.TrySetAccent(value.Hex, out _);
            }
        }
    }

    public string CustomAccentHex
    {
        get => _customAccentHex;
        set => SetProperty(ref _customAccentHex, value);
    }

    /// <summary>Erro de validação da cor personalizada (vazio quando não há erro).</summary>
    public string ValidationMessage
    {
        get => _validationMessage;
        private set => SetProperty(ref _validationMessage, value);
    }

    public DelegateCommand ApplyCustomAccentCommand { get; }

    public DelegateCommand RestoreDefaultsCommand { get; }

    private AccentOption FindAccentOption(string hex) =>
        AccentOptions.FirstOrDefault(option => option.Hex.Equals(hex, StringComparison.OrdinalIgnoreCase))
        ?? CustomAccent;

    private void ApplyCustomAccent()
    {
        if (!_appearanceService.TrySetAccent(CustomAccentHex, out var errorMessage))
        {
            ValidationMessage = errorMessage;
            return;
        }

        // Atualiza o campo sem passar pelo setter, que reaplicaria a cor.
        _selectedAccent = CustomAccent;
        OnPropertyChanged(nameof(SelectedAccent));
        CustomAccentHex = _appearanceService.Preferences.AccentHex;
        ClearValidationMessage();
    }

    private void RestoreDefaults()
    {
        _appearanceService.RestoreDefaults();

        // Os campos são alterados diretamente para não disparar SetMode/SetAccent de novo.
        _selectedMode = _appearanceService.Preferences.Mode;
        _customAccentHex = _appearanceService.Preferences.AccentHex;
        _selectedAccent = FindAccentOption(_customAccentHex);
        ClearValidationMessage();
        OnPropertyChanged(nameof(SelectedMode));
        OnPropertyChanged(nameof(CustomAccentHex));
        OnPropertyChanged(nameof(SelectedAccent));
    }

    private void ClearValidationMessage() => ValidationMessage = string.Empty;
}
