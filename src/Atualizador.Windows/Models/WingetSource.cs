namespace Atualizador.Windows.Models;

/// <summary>Fonte configurada no WinGet (linha de <c>winget source list</c>).</summary>
public sealed record WingetSource(string Name, string Argument, bool IsExplicit);
