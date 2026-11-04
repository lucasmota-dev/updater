using Atualizador.Windows.Models;

namespace Atualizador.Windows.Services;

/// <summary>
/// Códigos de saída do WinGet traduzidos em estado e mensagem para o usuário.
/// Fonte: tabela oficial "returnCodes.md" do repositório microsoft/winget-cli.
/// Só os códigos que mudam o que o usuário deve fazer estão aqui; os demais caem na mensagem genérica.
/// </summary>
public static class WingetErrorCodes
{
    public const int AccessDenied = 5;
    public const int NoApplicableInstaller = unchecked((int)0x8A150010);
    public const int InstallerHashMismatch = unchecked((int)0x8A150011);
    public const int CommandRequiresAdmin = unchecked((int)0x8A150019);
    public const int UpdateNotApplicable = unchecked((int)0x8A15002B);
    public const int UpgradeVersionUnknown = unchecked((int)0x8A150050);
    public const int InstallerProhibitsElevation = unchecked((int)0x8A150056);
    public const int PackageAlreadyInstalled = unchecked((int)0x8A150061);
    public const int AdminContextActionProhibited = unchecked((int)0x8A15007D);

    /// <summary>
    /// Estado e mensagem para um código conhecido, ou <c>null</c> se o código não estiver mapeado.
    /// <paramref name="operation"/> é "instalação" ou "atualização" (para o texto).
    /// </summary>
    public static (PackageUpdateStatus Status, string Message)? Describe(int exitCode, string operation) => exitCode switch
    {
        AccessDenied or CommandRequiresAdmin =>
            (PackageUpdateStatus.RequiresElevation, $"A {operation} exige permissão de administrador."),
        InstallerProhibitsElevation or AdminContextActionProhibited =>
            (PackageUpdateStatus.Failed, "Este instalador não aceita rodar como administrador. " + ElevationGuard.ElevationAdvice),

        UpdateNotApplicable or PackageAlreadyInstalled or unchecked((int)0x8A15010D) or unchecked((int)0x8A15010E) =>
            (PackageUpdateStatus.Ignored, "Já instalado: não há versão mais nova para este aplicativo."),
        UpgradeVersionUnknown =>
            (PackageUpdateStatus.Ignored, "A versão instalada é desconhecida; o WinGet não sabe se há atualização."),

        unchecked((int)0x8A150109) or unchecked((int)0x8A15010B) =>
            (PackageUpdateStatus.RestartRequired, "Concluído. Reinicie o computador para terminar."),
        unchecked((int)0x8A15010A) =>
            (PackageUpdateStatus.Failed, "Reinicie o computador antes de instalar este aplicativo."),

        unchecked((int)0x8A150101) or unchecked((int)0x8A150103) or unchecked((int)0x8A150111) =>
            (PackageUpdateStatus.Failed, "O aplicativo está aberto. Feche-o e tente de novo."),
        unchecked((int)0x8A150102) =>
            (PackageUpdateStatus.Failed, "Outra instalação do Windows está em andamento. Tente de novo quando ela terminar."),
        unchecked((int)0x8A15010C) =>
            (PackageUpdateStatus.Canceled, "A instalação foi cancelada no próprio instalador."),

        unchecked((int)0x8A150104) or unchecked((int)0x8A150110) =>
            (PackageUpdateStatus.Failed, "Falta um componente de que o aplicativo depende, e ele não pôde ser instalado."),
        unchecked((int)0x8A150105) => (PackageUpdateStatus.Failed, "Não há espaço suficiente em disco."),
        unchecked((int)0x8A150106) => (PackageUpdateStatus.Failed, "Não há memória suficiente para instalar."),
        unchecked((int)0x8A150107) => (PackageUpdateStatus.Failed, "O instalador precisa de conexão com a internet."),
        unchecked((int)0x8A15010F) => (PackageUpdateStatus.Failed, "Uma política da organização bloqueia esta instalação."),
        NoApplicableInstaller or unchecked((int)0x8A150113) =>
            (PackageUpdateStatus.Failed, "Este aplicativo não tem instalador compatível com este Windows."),
        unchecked((int)0x8A150114) =>
            (PackageUpdateStatus.Failed, "O instalador deste aplicativo não permite atualizar; desinstale e instale de novo."),
        InstallerHashMismatch =>
            (PackageUpdateStatus.Failed, "O arquivo baixado não confere com o catálogo do WinGet (hash diferente). Tente mais tarde."),

        _ => null
    };
}
