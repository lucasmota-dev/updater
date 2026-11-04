namespace Atualizador.Windows.ViewModels;

/// <summary>
/// Base das páginas exibidas no menu lateral. O <c>MainWindow.xaml</c> escolhe o layout de cada
/// página pelo tipo concreto (um <c>DataTemplate</c> por classe derivada).
/// </summary>
/// <param name="title">Nome no menu e título da página.</param>
/// <param name="description">Frase curta exibida abaixo do título.</param>
/// <param name="glyph">Ícone da fonte Segoe Fluent Icons / Segoe MDL2 Assets (código Unicode).</param>
public abstract class PageViewModel(string title, string description, string glyph) : ViewModelBase
{
    public string Title { get; } = title;

    public string Description { get; } = description;

    public string Glyph { get; } = glyph;
}
