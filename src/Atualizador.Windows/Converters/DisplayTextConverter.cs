using System.Globalization;
using System.Windows.Data;
using Atualizador.Windows.Models;

namespace Atualizador.Windows.Converters;

/// <summary>
/// Converte valores de enum em texto legível em português para as listas e o histórico.
/// Os nomes dos enums continuam sendo gravados nos arquivos JSON; só a exibição muda.
/// Uso no XAML: <c>{Binding Status, Converter={StaticResource DisplayTextConverter}}</c>.
/// </summary>
public sealed class DisplayTextConverter : IValueConverter
{
    private static readonly CultureInfo PortugueseCulture = CultureInfo.GetCultureInfo("pt-BR");

    public static string ToDisplayText(object? value) => value switch
    {
        AppearanceMode.Sistema => "Seguir o Windows",
        AppearanceMode.Claro => "Claro",
        AppearanceMode.Escuro => "Escuro",

        ScheduleMode.Desligado => "Desligado",
        ScheduleMode.ACadaHora => "A cada intervalo de horas",
        ScheduleMode.Diario => "Diário",
        ScheduleMode.Semanal => "Semanal",

        ScheduleScope.TodosElegiveis => "Todos os aplicativos elegíveis",
        ScheduleScope.PacotesSelecionados => "Somente os selecionados em Atualizações",

        HistoryOperation.Descoberta => "Verificação",
        HistoryOperation.Atualizacao => "Atualização",
        HistoryOperation.Instalacao => "Instalação",
        HistoryOperation.WindowsUpdate => "Windows Update",

        HistoryEntryStatus status => StatusText(status.ToString()),
        HistoryStatusFilter.Todos => "Todos os resultados",
        HistoryStatusFilter filter => StatusText(filter.ToString()),

        HistoryDateFilter.Todos => "Todas as datas",
        HistoryDateFilter.UltimosSeteDias => "Últimos 7 dias",
        HistoryDateFilter.UltimosTrintaDias => "Últimos 30 dias",

        DayOfWeek day => CapitalizeFirst(PortugueseCulture.DateTimeFormat.GetDayName(day)),

        _ => value?.ToString() ?? string.Empty
    };

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => ToDisplayText(value);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException("A conversão é apenas para exibição.");

    // "segunda-feira" → "Segunda-feira" (ToTitleCase deixaria "Segunda-Feira").
    private static string CapitalizeFirst(string text) =>
        text.Length == 0 ? text : char.ToUpper(text[0], PortugueseCulture) + text[1..];

    // HistoryEntryStatus e HistoryStatusFilter têm os mesmos nomes de membro; o texto é compartilhado.
    private static string StatusText(string name) => name switch
    {
        "Sucesso" => "Sucesso",
        "SemAtualizacoes" => "Sem atualizações",
        "Parcial" => "Parcial",
        "Falha" => "Falha",
        "Cancelado" => "Cancelado",
        _ => name
    };
}
