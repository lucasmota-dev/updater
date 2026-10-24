# Etapa 2 — Interface e temas

**Estado:** CONCLUÍDA
**Modelo recomendado:** Sonnet 5.5, esforço médio; Opus 5.5/alto para decisões visuais complexas.

## Objetivo

Criar uma interface com aparência moderna inspirada no Windows 11, com navegação clara, tema e personalização de cor.

## Passos

1. Ler a skill `windows-11-wpf-design`.
2. Inspecionar a implementação atual antes de alterar XAML ou recursos.
3. Pesquisar a documentação oficial atual do WPF para tema Fluent e APIs relacionadas. Confirmar se os recursos são estáveis na versão .NET alvo. Não depender de APIs experimentais sem registrar a limitação.
4. Avaliar se os estilos nativos são suficientes. Se uma biblioteca visual for proposta, verificar manutenção, licença, compatibilidade com .NET 10 e impacto no tamanho da publicação. Não adicionar biblioteca só por estética.
5. Criar um layout principal com navegação discreta: `Atualizações`, `Histórico` e `Configurações`. As páginas futuras podem ter conteúdo provisório, mas devem ser claramente identificadas como não implementadas.
6. Definir tokens/recursos centralizados para plano de fundo, superfície, texto, texto secundário, borda, cor de destaque, hover, seleção, sucesso, aviso e erro.
7. Implementar três modos de aparência: `Sistema`, `Claro` e `Escuro`.
8. Implementar cores predefinidas de destaque e cor personalizada por seletor ou entrada hexadecimal validada. Incluir ação `Restaurar padrão`.
9. Aplicar a cor selecionada a botões principais, foco, seleção, indicador de progresso e controles relevantes. Não colorir todos os elementos indiscriminadamente.
10. Persistir aparência e cor em configuração por usuário, sem exigir administrador.
11. Aplicar mudanças imediatamente sem reiniciar quando tecnicamente possível.
12. Conferir contraste, foco visível, teclado, escalonamento DPI e tamanho de janela redimensionável.
13. Usar efeitos translúcidos com moderação. Priorizar leitura e desempenho; não deixar texto ilegível em fundos com transparência.

## Critérios de aceite

- Tema Sistema/Claro/Escuro funciona.
- A cor de destaque pode ser escolhida, persistida e restaurada.
- Cor hexadecimal inválida é rejeitada sem quebrar o tema.
- O tema não depende de imagens remotas.
- Contraste, foco e navegação por teclado são utilizáveis.
- Preferências continuam após fechar e abrir o aplicativo.

## Verificações

Build; teste manual com cada tema; testar cor clara, escura e personalizada; redimensionar janela; navegar com Tab/Shift+Tab. Se o ambiente não for Windows, registrar a limitação visual.

## Resultado da execução

### Arquivos alterados

- `src/Atualizador.Windows/App.xaml` e `src/Atualizador.Windows/App.xaml.cs`.
- `src/Atualizador.Windows/Views/MainWindow.xaml` e `src/Atualizador.Windows/Views/MainWindow.xaml.cs`.
- `src/Atualizador.Windows/ViewModels/ViewModelBase.cs`, `DelegateCommand.cs` e `PageViewModels.cs`.
- `src/Atualizador.Windows/Models/AppearanceMode.cs`, `AccentOption.cs` e `AppearancePreferences.cs`.
- `src/Atualizador.Windows/Services/AppearanceService.cs` e `AppearancePreferencesStore.cs`.
- `src/Atualizador.Windows/Resources/Accent.xaml`, `Controls.xaml`, `Theme.Light.xaml` e `Theme.Dark.xaml`.
- `docs/00-plano-geral.md` e este arquivo.

### Decisão de design

Foi usada a infraestrutura nativa de `ResourceDictionary` do WPF com referências `DynamicResource`. A documentação oficial confirma que dicionários trocáveis são o mecanismo adequado para temas. O `ThemeMode`/Fluent nativo não foi usado porque a API ainda é marcada como experimental e o suporte Fluent do .NET 10 continua em evolução. Não foi adicionada biblioteca de terceiros.

### Verificações executadas

- `dotnet build Atualizador.Windows.slnx --configuration Release --no-restore`: concluído com 0 avisos e 0 erros.
- `dotnet test Atualizador.Windows.slnx --configuration Release --no-restore`: concluído com código 0; a solução ainda não possui projeto de testes.
- `problems` nos arquivos XAML, projeto, serviço de aparência e ViewModels: nenhum erro encontrado.
- Execução do `Atualizador.Windows.exe` no Windows: janela iniciada e mantida ativa durante as verificações.
- Automação nativa da interface: navegação até `Configurações`, seleção dos modos `Sistema`, `Claro` e `Escuro`, rejeição de `#GGGGGG`, aplicação de `#123456`, restauração padrão e persistência entre duas execuções.
- `git diff --check`: concluído sem erros.

### Limitações conhecidas

- O modo `Sistema` consulta a preferência de aplicativos do Windows quando a aparência é aplicada. Uma alteração externa do tema enquanto o aplicativo está aberto será refletida na próxima aplicação do modo `Sistema`; não foi adicionada escuta global para evitar dependências extras nesta etapa.
- A barra de título permanece sob o controle padrão do WPF/Windows. A API experimental `ThemeMode` não foi usada.
- As páginas `Atualizações` e `Histórico` são apenas estados provisórios; nenhuma integração funcional das próximas etapas foi antecipada.
