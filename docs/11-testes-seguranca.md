# Etapa 11 — Testes e segurança

**Estado:** CONCLUÍDA
**Modelo recomendado:** Opus 5.5, esforço alto.

## Objetivo

Validar fluxos centrais, permissões, tratamento de erros e ausência de atualizações reais durante testes automáticos.

## Passos

1. Ler `thermo-nuclear-code-quality-review` e `winget-safe-execution`.
2. Executar o conjunto de testes sem instalar, atualizar ou remover software real.
3. Testar saída do processo vazia, saída extensa, erro, timeout, cancelamento e código de saída não zero.
4. Testar IDs malformados, strings com aspas, espaços e caracteres especiais; confirmar que `ArgumentList` evita injeção de shell.
5. Testar WinGet ausente, fontes indisponíveis, Store indisponível, nenhuma atualização e pacote com versão desconhecida.
6. Testar seleção individual, lote, filtro, cancelamento, atualização concorrente e bloqueio por tarefa ativa.
7. Testar tema Sistema/Claro/Escuro, cor inválida, persistência e valor padrão.
8. Testar arquivo de histórico ausente, corrompido e gravado parcialmente.
9. Testar criação/remoção de tarefa sem disparar uma atualização real.
10. Revisar privilégios, caminhos graváveis, logs, chamadas de processo e configuração de início automático.
11. Executar build Release e revisar warnings relevantes.
12. Fazer revisão estrutural de simplificação. Corrigir bloqueadores antes da etapa final.

## Critérios de aceite

- Testes automáticos usam executores simulados.
- Nenhum segredo ou dado desnecessário é registrado.
- O aplicativo roda sem elevação inicial.
- Não há bloqueadores de qualidade conhecidos sem justificativa documentada.
- Limitações do teste fora de Windows estão listadas.

## Resultado da execução

### Comandos e resultados reais

- `dotnet build Atualizador.Windows.slnx --configuration Release --no-restore`: 0 avisos e 0 erros.
- `dotnet test tests/Atualizador.Windows.Tests/Atualizador.Windows.Tests.csproj --configuration Release --no-build`: 21 testes aprovados, 0 falhas.
- `problems` nos arquivos principais: nenhum erro encontrado.
- Aplicativo WPF Release iniciado no Windows e encerrado sem falha.
- Executável publicado `win-x64` iniciado e encerrado sem falha.
- WinGet real foi usado somente para `--version`, `source list` e `list --upgrade-available`; nenhuma atualização/instalação foi executada.
- Task Scheduler real foi usado somente com tarefa temporária criada/desativada/reativada/removida; a tarefa não foi disparada.

### Cobertura de segurança

- Testes usam `IProcessExecutor` e localizador falsos para não executar WinGet real.
- IDs e origens inválidos são rejeitados antes de montar a atualização.
- Argumentos são adicionados individualmente por `ArgumentList`; não há `cmd.exe /c`, PowerShell de comando ou argumentos livres na aplicação.
- `--all` não é usado para seleção parcial e `--include-unknown` só aparece mediante opção explícita.
- Testes cobrem saída localizada, campos ausentes, duplicidade, erro de fonte, código não zero, cancelamento, persistência corrompida, seleção, tarefa e Store.
- Nenhum segredo, token ou stdout/stderr integral é persistido.

### Limitações não classificadas como aprovadas

- Não foi feito teste em VM limpa sem SDK; a publicação foi validada no Windows de desenvolvimento.
- Não foi executada atualização real de aplicativo; isso exige autorização manual explícita.
- Não foi testado usuário desconectado nem todos os estados possíveis da Microsoft Store.
