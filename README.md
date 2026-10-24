# Atualizador Windows

Instruções por etapas para desenvolver um atualizador de aplicativos Windows com **C# + WPF + .NET 10 LTS** e apoio do Claude Code.

## Objetivo

Criar um aplicativo desktop simples e moderno que use o WinGet para detectar e atualizar pacotes, permita selecionar aplicativos e ofereça execução agendada pelo Agendador de Tarefas do Windows. A integração com a Microsoft Store deve respeitar as limitações do WinGet e do contexto da sessão do usuário.

## Como trabalhar com estes arquivos

1. Abra esta pasta como repositório no Claude Code.
2. Leia `CLAUDE.md` e este `README.md`.
3. Peça ao Claude para executar **somente** `docs/01-base-projeto.md`.
4. Valide build, testes e resultado visual antes de avançar.
5. Continue uma etapa por vez, seguindo a ordem de `docs/00-plano-geral.md`.

Prompt inicial sugerido:

> Leia `CLAUDE.md`, `README.md`, as skills relevantes e `docs/00-plano-geral.md`. Execute somente `docs/01-base-projeto.md`. Não antecipe outras etapas. Inspecione o ambiente, implemente o escopo mínimo, execute as verificações disponíveis e registre resultados reais.

## Tecnologias e decisões

- **Linguagem:** C#.
- **Interface:** WPF, XAML e padrão MVVM simples.
- **Runtime alvo:** `net10.0-windows`, após confirmar que o SDK .NET 10 está disponível.
- **Distribuição:** publicação autocontida para `win-x64`; testar outras arquiteturas separadamente.
- **Atualizações:** executar o cliente `winget` por meio de `Process` com argumentos separados. Nunca usar `cmd.exe /c` para montar comandos com texto do usuário.
- **Agendamento:** começar pelo Agendador de Tarefas do Windows. Não criar serviço Windows na primeira versão.
- **Configuração:** salvar preferências do usuário em `%LOCALAPPDATA%`, com arquivo simples e versionado.
- **Interface:** estilo Windows 11; tema Sistema/Claro/Escuro, cor de destaque selecionável e opção de cor personalizada.
- **Idioma:** português brasileiro por padrão.

.NET 10 é LTS e, segundo a política oficial consultada em 24/10/2026, tem suporte previsto até 14/11/2028. Confirmar a política vigente antes de cada atualização importante: https://dotnet.microsoft.com/pt-br/platform/support/policy

## Estrutura

```text
.
├── CLAUDE.md
├── README.md
├── docs/                     # Etapas de implementação
└── .claude/
    ├── agents/               # Especialistas delegáveis
    └── skills/               # Padrões permanentes do projeto
```

O código da aplicação será criado durante as etapas. Não é necessário gerar uma arquitetura completa antes de validar a primeira janela.

## Modelo e esforço para o Claude

- **Padrão:** Claude Sonnet 5.5, esforço médio para implementação delimitada.
- **Arquitetura, segurança, problemas difíceis e revisão final:** Claude Opus 5.5, esforço alto.
- **Tarefas auxiliares curtas:** Claude Haiku 5.5, quando disponível.

Essas recomendações refletem os modelos anunciados pela Anthropic até 24/10/2026; a disponibilidade depende da sua conta e da versão do Claude Code. Referência: https://www.anthropic.com/news/models

## Limites importantes

- Não assumir que todos os aplicativos da Microsoft Store podem ser atualizados pelo WinGet.
- Não executar atualizações reais durante testes automatizados.
- Não fingir uma porcentagem de progresso que o WinGet não fornece. Exibir o aplicativo atual e o progresso geral por quantidade; usar progresso indeterminado durante a instalação.
- Não exigir privilégios administrativos para abrir o aplicativo. Tratar separadamente instalações que pedirem elevação.
- A tarefa agendada deve usar inicialmente a conta do usuário configurada e o contexto de sessão compatível com WinGet/Store. Documentar limitações para execução com sessão encerrada.
- Não prometer que um único `.exe` remove a dependência do WinGet do computador de destino.

## Referências oficiais

- WinGet: https://learn.microsoft.com/pt-br/windows/package-manager/winget/
- Comando `upgrade`: https://learn.microsoft.com/pt-br/windows/package-manager/winget/upgrade
- Fontes do WinGet, incluindo `msstore`: https://learn.microsoft.com/pt-br/windows/package-manager/winget/source
- WPF: https://learn.microsoft.com/pt-br/dotnet/desktop/wpf/overview/
- Publicação de arquivo único: https://learn.microsoft.com/pt-br/dotnet/core/deploying/single-file/overview
- Claude Code Skills: https://docs.anthropic.com/en/docs/claude-code/skills
- Agentes no Claude Code: https://docs.anthropic.com/en/docs/claude-code/sub-agents

## Estado atual do aplicativo

As etapas 01 a 12 foram implementadas e verificadas no Windows. A solução está em `Atualizador.Windows.slnx`, o projeto WPF está em `src/Atualizador.Windows` e os testes estão em `tests/Atualizador.Windows.Tests`.

O aplicativo oferece:

- interface WPF em português brasileiro, com temas Sistema, Claro e Escuro;
- estilo de botões inspirado no WinHawk / Windows 11 Fluent, com cantos arredondados, foco por teclado e contraste revisado;
- cor de destaque do sistema, predefinida ou hexadecimal personalizada;
- descoberta de atualizações com WinGet, sem alterar fontes automaticamente;
- exclusão de aplicativos da atualização com persistência local e gestão exclusiva em Configurações;
- pesquisa e instalação de novos pacotes pelo WinGet com seleção múltipla, progresso e avisos de procedência;
- seleção, filtro por origem, progresso, cancelamento e confirmação antes da atualização ou instalação;
- histórico JSON local em `%LOCALAPPDATA%\\Atualizador.Windows`;
- agendamento e inicialização opcional por tarefas fixas e removíveis do Agendador de Tarefas, respeitando as exclusões;
- ícone local baseado no Material Symbols `system_update` e área de notificação com menu acessível;
- abertura da página de atualizações da Microsoft Store para itens da origem `msstore`.

## Requisitos

- Windows 10 ou Windows 11 x64.
- WinGet/App Installer para descoberta e atualização de aplicativos.
- Sessão de usuário compatível com WinGet e Microsoft Store para operações que exigem interação ou acordos.

O aplicativo não incorpora o WinGet e não exige privilégios administrativos para abrir.

## Executar no desenvolvimento

```powershell
dotnet run --project src/Atualizador.Windows/Atualizador.Windows.csproj
```

Executar os testes sem atualizar aplicativos reais:

```powershell
dotnet test tests/Atualizador.Windows.Tests/Atualizador.Windows.Tests.csproj --configuration Release
```

## Publicar

Publicação autocontida para Windows x64:

```powershell
dotnet restore src/Atualizador.Windows/Atualizador.Windows.csproj --runtime win-x64
dotnet publish src/Atualizador.Windows/Atualizador.Windows.csproj --configuration Release --no-restore --output artifacts/publish/win-x64 -p:PublishProfile=WinX64
```

O pacote ZIP gerado em `artifacts/Atualizador.Windows-0.1.0-win-x64.zip` contém o executável autocontido. A publicação não é assinada nesta versão.

O instalador `Atualizador-Setup.exe` é compilado pelo workflow do GitHub Actions com `installer/Atualizador.iss` e instala em `C:\Atualizador` com atalhos e desinstalador.

## Instalação e remoção

1. Execute `Atualizador-Setup.exe` para instalar em `C:\Atualizador`, ou extraia o ZIP portátil em uma pasta escolhida pelo usuário.
2. O instalador cria atalhos na Área de Trabalho e no Menu Iniciar.
3. Configure o agendamento, a inicialização e a bandeja somente se desejar.
4. Para remover, desative/remova o agendamento e a inicialização na tela **Configurações**.
5. Use o desinstalador do Inno Setup ou remova a pasta portátil. As preferências e o histórico ficam em `%LOCALAPPDATA%\\Atualizador.Windows` e não devem ser removidos automaticamente.

## Limitações confirmadas

- A saída do WinGet é uma tabela de texto; o parser cobre formatos testados em inglês, português e espanhol, mas pode exigir evolução para novos formatos.
- `msstore` não representa todos os aplicativos instalados pela Microsoft Store.
- Instaladores podem exigir UAC, interface própria ou reinicialização; cancelar o WinGet não garante rollback.
- Agendamentos dependem da conta atual, da sessão compatível e das limitações do WinGet/Store quando o usuário está desconectado.
- A barra de progresso representa pacotes processados; durante o instalador individual ela é indeterminada.
- A publicação não inclui assinatura de código.
- O compilador Inno Setup não estava disponível localmente; a compilação do instalador foi preparada no workflow e não foi validada neste checkout.
- Não execute `Atualizador.Windows.exe --scheduled` em um ambiente com agendamento ativo durante testes: esse modo pode iniciar atualizações reais. Use uma configuração desligada ou um ambiente isolado.
