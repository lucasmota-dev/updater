# Etapa 4 — Lista e seleção de aplicativos

**Estado:** CONCLUÍDA
**Modelo recomendado:** Sonnet 5.5, esforço médio.

## Objetivo

Exibir as atualizações descobertas e permitir que o usuário escolha exatamente quais pacotes pretende atualizar.

## Passos

1. Criar um modelo tipado para pacote atualizável, incluindo ID, origem e versões; não armazenar apenas nome e versão.
2. Criar uma lista virtualizável ou eficiente para listas maiores, com ícone local quando disponível e ícone genérico como fallback.
3. Exibir nome, versão instalada, versão disponível e origem (`WinGet` ou `Microsoft Store`, quando confirmada).
4. Implementar seleção individual, selecionar todos, limpar seleção e filtros por origem.
5. Manter a seleção correta ao filtrar, ordenar e atualizar a lista.
6. Exibir estados vazios e de erro. Mostrar claramente quando não foi possível determinar a versão.
7. Não baixar imagens de ícones da internet em tempo de execução. Se não houver ícone confiável, usar o ícone genérico.
8. Criar ação `Atualizar selecionados`, desativada sem seleção ou enquanto uma operação estiver em andamento.
9. Mostrar confirmação antes de uma atualização em lote, com quantidade e escopo.
10. Não iniciar instalação ainda se a etapa 5 não estiver concluída.

## Critérios de aceite

- A seleção é preservada ao filtrar.
- A lista usa ID e origem do serviço de descoberta.
- O usuário consegue selecionar um, vários ou todos.
- Estados vazios e erros são claros.
- A ação de atualização não executa processos nesta etapa ou permanece ligada ao serviço simulado.

## Resultado da execução

### Arquivos alterados

- `src/Atualizador.Windows/Models/PackageSourceFilter.cs`.
- `src/Atualizador.Windows/ViewModels/AsyncDelegateCommand.cs`, `UpdatesPageViewModel.cs` e `PageViewModels.cs`.
- `src/Atualizador.Windows/Views/MainWindow.xaml` e `src/Atualizador.Windows/Resources/Controls.xaml`.
- `docs/00-plano-geral.md` e este arquivo.

### Verificações executadas

- `dotnet build Atualizador.Windows.slnx --configuration Release --no-restore`: concluído sem avisos ou erros.
- `dotnet test tests/Atualizador.Windows.Tests/Atualizador.Windows.Tests.csproj --configuration Release --no-build`: 9 testes aprovados.
- Teste manual no Windows: consulta real exibiu `Microsoft Edge`.
- Seleção individual atualizou o contador e permaneceu preservada nos filtros `WinGet` e `Microsoft Store`.
- Estado vazio para filtro sem resultados foi exibido.
- `Atualizar selecionados` permaneceu desabilitado; nenhum processo de instalação foi iniciado.

### Limitações e escopo

- A execução de atualização ainda não está ligada à interface. A ação permanece desabilitada até a etapa 5.
- A lista usa virtualização WPF com `ListView` e `Recycling`; ícones locais ainda não foram adicionados porque a descoberta não fornece um ícone confiável nesta etapa.
