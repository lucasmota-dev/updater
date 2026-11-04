# App Updater

Aplicativo para Windows que mantém os seus programas atualizados usando o **WinGet**, o gerenciador de pacotes oficial do Windows. Ele mostra quais aplicativos têm versão nova, atualiza os que você escolher e também pode fazer isso sozinho, em um horário agendado.

Feito em C#, WPF e .NET 10. Interface em português do Brasil.

## Recursos

- **Atualizações:** ao abrir, lista os aplicativos instalados que têm versão mais nova no WinGet. Você atualiza os marcados ou todos de uma vez e acompanha o progresso de cada um.
- **Instalar:** pesquisa aplicativos no catálogo do WinGet, mesmo com o nome aproximado ("firefox br", "firfox"), e instala os selecionados.
- **Windows Update:** procura e instala as atualizações do próprio Windows. A permissão de administrador é pedida só na hora de instalar, e o app avisa quando é preciso reiniciar.
- **Exclusões:** aplicativos que você não quer atualizar (por exemplo, uma versão específica de que você depende) ficam fora das verificações, manuais ou agendadas.
- **Agendamento:** atualização automática a cada intervalo de horas, diária ou semanal, por uma tarefa do Agendador de Tarefas do Windows.
- **Histórico:** registro local de todas as verificações, atualizações e instalações.
- **Aparência:** tema claro, escuro ou igual ao do Windows; cor de destaque do sistema, predefinida ou personalizada; menu lateral que pode ser recolhido.
- **Área de notificação:** opção de continuar rodando na bandeja ao fechar a janela.
- **Atualização do próprio app:** ao abrir, o App Updater procura uma versão nova no GitHub e se atualiza sozinho. Também há o botão **Buscar nova versão** em Configurações.

## Requisitos

- Windows 10 ou Windows 11, 64 bits.
- [WinGet / Instalador de Aplicativo](https://learn.microsoft.com/windows/package-manager/winget/) instalado (já vem no Windows 11 e nas versões atuais do Windows 10).

O App Updater não precisa de permissão de administrador para abrir. Alguns instaladores de terceiros podem pedir essa permissão durante a atualização.

## Instalação

1. Baixe `Atualizador-Setup.exe` na [página de releases](https://github.com/lucasmota-dev/updater/releases/latest).
2. Execute o instalador. Ele instala só para o seu usuário, em `%LOCALAPPDATA%\Programs\Atualizador`, e cria atalhos na Área de Trabalho e no Menu Iniciar.

Também existe a versão portátil, `Atualizador.exe`, que não precisa de instalação. A versão portátil não se atualiza sozinha: ela só avisa quando há versão nova.

Cada release publica `SHA256SUMS.txt` com as somas SHA-256 dos arquivos, para você conferir o download:

```powershell
Get-FileHash .\Atualizador-Setup.exe -Algorithm SHA256
```

> **Aviso do Windows SmartScreen:** enquanto os arquivos não forem assinados (veja [Política de assinatura de código](#política-de-assinatura-de-código)), o Windows pode mostrar "O Windows protegeu o computador". Para continuar, clique em **Mais informações** e depois em **Executar assim mesmo**.

## Desinstalação

1. Em **Configurações**, desative o agendamento e a inicialização com o Windows, se você os ativou.
2. Desinstale pelo painel **Aplicativos instalados** do Windows.

As preferências e o histórico ficam em `%LOCALAPPDATA%\Atualizador.Windows` e não são removidos automaticamente. Apague essa pasta se quiser remover tudo.

## Segurança

- O app executa o `winget.exe` com argumentos separados, sem passar pelo `cmd.exe`, então nada do que você digita vira um comando.
- Atualização e instalação usam sempre o **ID exato** e a **origem** do pacote (`--id … --exact --source …`), nunca só o nome exibido.
- Só a fonte `winget` é consultada. A Microsoft Store (`msstore`) não é suportada.
- As tarefas agendadas rodam com a sua conta, sem elevação, e sempre executam o mesmo comando fixo.
- Para instalar atualizações do Windows, o app abre uma cópia de si mesmo como administrador (aviso do UAC) com um argumento fixo. Essa cópia não recebe nenhum outro dado da janela e envia o progresso por um *named pipe*.
- A atualização do próprio app só aceita downloads de `github.com` e confere o SHA-256 do instalador antes de executá-lo.

Cancelar uma atualização interrompe o WinGet, mas um instalador que já começou pode não desfazer o que fez.

## Privacidade

O App Updater não coleta dados pessoais, não usa telemetria e não envia a lista dos seus aplicativos para nenhum servidor. Ele só se conecta à internet nestes casos:

1. **WinGet:** ao abrir a página Atualizações e quando você busca, pesquisa ou instala aplicativos, o WinGet consulta as fontes configuradas no Windows e baixa os instaladores. Essas conexões seguem a política de privacidade da Microsoft e dos publicadores de cada aplicativo.
2. **Nova versão do App Updater:** ao abrir, o app consulta as releases deste repositório no GitHub (`api.github.com`). Para desligar essa consulta, desmarque **Procurar nova versão ao abrir o aplicativo** na página **Sobre**. Essa conexão segue a [política de privacidade do GitHub](https://docs.github.com/site-policy/privacy-policies/github-general-privacy-statement).
3. **Windows Update:** ao abrir a página Windows Update e ao instalar, o Windows Update Agent do próprio Windows consulta e baixa as atualizações dos servidores da Microsoft, como a tela Configurações do Windows faz.

Preferências e histórico ficam só no seu computador. O instalador mostra este aviso antes de instalar (`installer/PRIVACIDADE.txt`).

## Limitações conhecidas

- O WinGet não oferece saída estruturada para essas listas, então o app lê as tabelas de texto. O formato foi testado em inglês, português e espanhol; versões futuras do WinGet podem exigir ajustes.
- Atualizações agendadas dependem de você estar conectado ao Windows.
- Alguns instaladores exigem interação ou reinicialização e não completam em modo silencioso.

## Desenvolvimento

Requer o [.NET SDK 10](https://dotnet.microsoft.com/download).

```powershell
# Executar
dotnet run --project src/Atualizador.Windows/Atualizador.Windows.csproj

# Testes (usam processos simulados: nenhum aplicativo real é atualizado)
dotnet test tests/Atualizador.Windows.Tests/Atualizador.Windows.Tests.csproj

# Publicar o executável autocontido
dotnet publish src/Atualizador.Windows/Atualizador.Windows.csproj -c Release -p:PublishProfile=WinX64 -o artifacts/publish/win-x64
```

> Não execute `Atualizador.Windows.exe --scheduled` em uma máquina com agendamento configurado: esse modo atualiza aplicativos de verdade.

### Estrutura

```text
src/Atualizador.Windows/
├── Models/        Dados (pacotes, histórico, preferências)
├── Services/      WinGet, Agendador de Tarefas, arquivos JSON, tema, atualização do app
├── ViewModels/    Lógica das telas (MVVM)
├── Views/         Janela principal (XAML)
├── Converters/    Textos de exibição
└── Resources/     Temas, cores e estilos dos controles
tests/Atualizador.Windows.Tests/   Testes automatizados (MSTest)
installer/Atualizador.iss          Instalador (Inno Setup)
tools/                             Scripts do CI e do release
```

O nome técnico do projeto (namespace, executável, pasta de dados e nomes das tarefas agendadas) continua `Atualizador.Windows`. Isso mantém compatíveis as instalações, os agendamentos e as atualizações automáticas de quem já usa o app.

### Versões e releases

- Versão no formato `yyyy.MM.dd.NN` (data no horário de Brasília + contador do dia), por exemplo `2026.11.04.01`. A tag Git é `v2026.11.04.01`.
- A versão não é escrita à mão: `tools/Get-NextVersion.ps1` a calcula no workflow de release. Compilações locais ficam em `0.0.0.0` e não procuram atualização.
- O workflow **CI** compila e testa cada pull request e cada push no `main`. Quando o CI termina com sucesso em um push no `main`, o workflow **Release** compila, testa, gera `Atualizador.exe`, `Atualizador-Setup.exe` e `SHA256SUMS.txt` e publica a release.

### Configuração da assinatura (mantenedores)

O workflow **Release** assina `Atualizador.exe` e `Atualizador-Setup.exe` pelo [SignPath](https://signpath.io). As etapas só rodam quando estes valores existem em **Settings › Secrets and variables › Actions**:

| Tipo | Nome |
|---|---|
| Segredo | `SIGNPATH_API_TOKEN` |
| Variável | `SIGNPATH_ORGANIZATION_ID`, `SIGNPATH_PROJECT_SLUG`, `SIGNPATH_SIGNING_POLICY_SLUG` |

As configurações de artefato usadas no SignPath estão em `.signpath/artifact-configurations/` (`app-exe` e `installer`). Cada assinatura precisa ser aprovada manualmente no SignPath; o workflow espera até 1 hora por aprovação. Sem esses valores, os binários são publicados sem assinatura.

## Política de assinatura de código

Free code signing provided by [SignPath.io](https://about.signpath.io), certificate by [SignPath Foundation](https://signpath.org).

- **Autores e revisores (committers and reviewers):** [Lucas Mota](https://github.com/lucasmota-dev). Contribuições de outras pessoas só entram depois da revisão dele.
- **Aprovadores (approvers):** [Lucas Mota](https://github.com/lucasmota-dev). Cada assinatura é aprovada manualmente.
- Só são assinados binários compilados pelo GitHub Actions a partir do código deste repositório.
- Privacidade: veja a seção [Privacidade](#privacidade). O app não envia informações a outros sistemas, exceto as consultas descritas lá, e a consulta automática de nova versão pode ser desligada.

## Licença

[MIT](LICENSE) © Lucas Mota

## Créditos

- Ícone baseado no símbolo `system_update` do [Material Symbols](https://fonts.google.com/icons), licença Apache 2.0 (`assets/icons/Apache-2.0.txt`).

## Autor

Lucas Mota — [github.com/lucasmota-dev](https://github.com/lucasmota-dev)
