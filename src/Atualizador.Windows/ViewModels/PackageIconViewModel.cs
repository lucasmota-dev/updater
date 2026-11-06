using System.Windows.Media;
using Atualizador.Windows.Services;

namespace Atualizador.Windows.ViewModels;

/// <summary>
/// Ícone de um pacote na lista. Começa com a inicial do nome e troca pela imagem quando ela é encontrada.
/// <para>
/// A busca só começa quando a tela lê <see cref="Image"/> pela primeira vez. Como as listas são
/// virtualizadas, só as linhas visíveis buscam ícone; rolar a lista busca as próximas.
/// </para>
/// </summary>
public sealed class PackageIconViewModel(string name, string id, PackageIconService? iconService) : ViewModelBase
{
    private ImageSource? _image;
    private bool _requested;

    /// <summary>Primeira letra ou dígito do nome, exibida enquanto não há imagem (ou se ela não existir).</summary>
    public string Initial { get; } = name.FirstOrDefault(char.IsLetterOrDigit) is var first && first != default
        ? char.ToUpperInvariant(first).ToString()
        : "?";

    public ImageSource? Image
    {
        get
        {
            if (!_requested && iconService is not null)
            {
                _requested = true;
                _ = LoadAsync(iconService);
            }

            return _image;
        }
        private set
        {
            if (SetProperty(ref _image, value))
            {
                OnPropertyChanged(nameof(HasImage));
            }
        }
    }

    public bool HasImage => _image is not null;

    private async Task LoadAsync(PackageIconService service) => Image = await service.GetIconAsync(name, id);
}
