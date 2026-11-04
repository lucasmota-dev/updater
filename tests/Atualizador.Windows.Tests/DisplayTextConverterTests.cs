using Atualizador.Windows.Converters;
using Atualizador.Windows.Models;

namespace Atualizador.Windows.Tests;

[TestClass]
public sealed class DisplayTextConverterTests
{
    [TestMethod]
    public void ToDisplayText_ShowsPortugueseLabelsInsteadOfEnumNames()
    {
        Assert.AreEqual("Últimos 7 dias", DisplayTextConverter.ToDisplayText(HistoryDateFilter.UltimosSeteDias));
        Assert.AreEqual("Sem atualizações", DisplayTextConverter.ToDisplayText(HistoryEntryStatus.SemAtualizacoes));
        Assert.AreEqual("Sem atualizações", DisplayTextConverter.ToDisplayText(HistoryStatusFilter.SemAtualizacoes));
        Assert.AreEqual("Todos os resultados", DisplayTextConverter.ToDisplayText(HistoryStatusFilter.Todos));
        Assert.AreEqual("Atualização", DisplayTextConverter.ToDisplayText(HistoryOperation.Atualizacao));
        Assert.AreEqual("Diário", DisplayTextConverter.ToDisplayText(ScheduleMode.Diario));
        Assert.AreEqual("Seguir o Windows", DisplayTextConverter.ToDisplayText(AppearanceMode.Sistema));
    }

    [TestMethod]
    public void ToDisplayText_DayOfWeekUsesPortugueseWithCapitalFirstLetter()
    {
        Assert.AreEqual("Segunda-feira", DisplayTextConverter.ToDisplayText(DayOfWeek.Monday));
        Assert.AreEqual("Sábado", DisplayTextConverter.ToDisplayText(DayOfWeek.Saturday));
    }

    [TestMethod]
    public void ToDisplayText_UnknownValuesFallBackToToString()
    {
        Assert.AreEqual("12", DisplayTextConverter.ToDisplayText(12));
        Assert.AreEqual(string.Empty, DisplayTextConverter.ToDisplayText(null));
    }
}
