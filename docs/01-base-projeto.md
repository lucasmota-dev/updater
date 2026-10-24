# Etapa 1 — Base do projeto

**Estado:** CONCLUÍDA
**Modelo recomendado:** Sonnet 5.5, esforço médio. Use Opus 5.5/alto se houver decisão arquitetural difícil.

## Objetivo

Criar a solução WPF mínima, compilável e pronta para evoluir. Não implementar atualização do WinGet nesta etapa.

## Passos

1. Inspecionar a pasta, o estado Git e as ferramentas instaladas. Preservar arquivos existentes.
2. Confirmar a versão do SDK .NET com `dotnet --info` e `dotnet --list-sdks`.
3. Consultar a documentação oficial e usar `net10.0-windows` se o SDK correspondente estiver instalado. Não instalar SDK globalmente sem autorização. Se não estiver disponível, documentar o bloqueio e escolher a alternativa suportada mais próxima apenas para validar a base.
4. Criar uma solução e um projeto WPF com nomes consistentes, por exemplo `Atualizador.Windows.sln` e `src/Atualizador.Windows/Atualizador.Windows.csproj`.
5. Criar a janela principal com título, tamanho inicial adequado e conteúdo de boas-vindas simples.
6. Criar a estrutura mínima para `Models`, `ViewModels`, `Views`, `Services` e `Resources` somente quando cada pasta tiver uso real. Não criar pastas vazias desnecessárias.
7. Configurar nullable reference types e análise básica do compilador sem adicionar regras que bloqueiem desnecessariamente a primeira compilação.
8. Adicionar um ponto de entrada e um ViewModel mínimo para confirmar binding.
9. Definir nome provisório do aplicativo como `Atualizador Windows`; não criar logotipo complexo nesta etapa.
10. Executar `dotnet build` e corrigir erros.

## Restrições

- Não adicionar banco de dados.
- Não adicionar bibliotecas de terceiros sem necessidade demonstrada.
- Não executar `winget upgrade`.
- Não adicionar serviço Windows, agendamento ou inicialização automática.
- Não construir toda a arquitetura antes de haver necessidade real.

## Critérios de aceite

- A solução compila.
- A janela abre em Windows com conteúdo inicial.
- Existe uma separação mínima entre janela e estado exibido.
- O projeto usa o alvo .NET documentado, ou a limitação do SDK está registrada.
- Nenhuma atualização de aplicativo foi executada.

## Verificações

Executar `dotnet build <caminho-da-solução>` e inspecionar erros/avisos. Testar abrir a janela se o ambiente for Windows.

## Resultado da execução

### Arquivos alterados

- `AGENTS.md` com instruções permanentes adaptadas ao Codex.
- `Atualizador.Windows.slnx` e `src/Atualizador.Windows/Atualizador.Windows.csproj`.
- `src/Atualizador.Windows/App.xaml` e `src/Atualizador.Windows/App.xaml.cs`.
- `src/Atualizador.Windows/Views/MainWindow.xaml` e `src/Atualizador.Windows/Views/MainWindow.xaml.cs`.
- `src/Atualizador.Windows/ViewModels/MainWindowViewModel.cs`.
- `src/Atualizador.Windows/Resources/Theme.Light.xaml`, `Theme.Dark.xaml` e `Accent.xaml`.
- `docs/00-plano-geral.md` e este arquivo.

### SDK encontrado

- Windows x64.
- SDK .NET `10.0.402`.
- Runtime WPF `Microsoft.WindowsDesktop.App 10.0.13`.
- Alvo do projeto: `net10.0-windows`.

### Comandos executados e resultados

- `dotnet --info`: concluído; confirmou o SDK `10.0.402` e o runtime WPF.
- `dotnet --list-sdks`: concluído; listou `10.0.402`.
- `dotnet new sln --name Atualizador.Windows`: concluído; criou `Atualizador.Windows.slnx`.
- `dotnet new wpf --name Atualizador.Windows --output src/Atualizador.Windows --framework net10.0 --no-restore`: concluído; o template não aceita `net10.0-windows` diretamente, e o projeto foi ajustado para esse alvo.
- `dotnet sln Atualizador.Windows.slnx add src/Atualizador.Windows/Atualizador.Windows.csproj`: concluído.
- `dotnet build Atualizador.Windows.slnx --configuration Release --no-restore`: concluído com 0 avisos e 0 erros.
- `dotnet test Atualizador.Windows.slnx --configuration Release --no-restore`: concluído com código 0; não há projeto de testes na solução.
- Execução do `Atualizador.Windows.exe` em Release: processo iniciado no Windows, permaneceu ativo durante a verificação e foi encerrado em seguida.

### Limitações e escopo

- O tema claro está ativo nesta etapa. O dicionário de tema escuro e o dicionário de destaque já estão separados para a etapa de interface e temas; a troca de tema, seleção de cor e persistência ainda não foram implementadas.
- Nenhuma atualização, integração com WinGet, agendamento ou serviço Windows foi executado ou criado.
- Não há testes automatizados porque a etapa não contém lógica de serviço ou projeto de testes.
