# Etapa 6 — Histórico e registros

**Estado:** CONCLUÍDA
**Modelo recomendado:** Sonnet 5.5, esforço médio.

## Objetivo

Guardar histórico local compreensível das verificações e das atualizações.

## Passos

1. Definir um modelo simples de execução: data, início/fim, origem, quantidade, status geral e resultado por pacote.
2. Salvar em `%LOCALAPPDATA%\<NomeDoApp>\`, sem precisar de administrador.
3. Escolher JSON ou SQLite somente após avaliar volume e necessidade. Para o primeiro MVP, preferir JSON versionado se o histórico permanecer pequeno.
4. Escrever dados de forma resiliente para reduzir risco de arquivo parcial. Tratar arquivo corrompido sem travar a interface.
5. Criar tela de histórico com filtro por data/status, detalhes do pacote e mensagem resumida de erro.
6. Aplicar limite configurável ou política documentada de retenção. Não apagar registros inesperadamente.
7. Registrar exceções e metadados úteis, sem conteúdo ilimitado de stdout/stderr.
8. Oferecer ação para abrir a pasta de logs e limpar o histórico com confirmação.
9. Não incluir senhas, tokens, informações pessoais desnecessárias ou dados completos do ambiente.

## Critérios de aceite

- Histórico permanece após reiniciar a aplicação.
- Arquivo corrompido não impede abrir o app.
- Logs diferenciam erro de descoberta e erro de atualização.
- Limpeza exige confirmação e explica o alcance.

## Resultado da execução

### Arquivos alterados

- `src/Atualizador.Windows/Models/HistoryModels.cs`.
- `src/Atualizador.Windows/Services/HistoryStore.cs`.
- `src/Atualizador.Windows/ViewModels/HistoryPageViewModel.cs`, `UpdatesPageViewModel.cs` e `PageViewModels.cs`.
- `src/Atualizador.Windows/Views/MainWindow.xaml`.
- `tests/Atualizador.Windows.Tests/HistoryStoreTests.cs`.
- `docs/00-plano-geral.md` e este arquivo.

### Formato e política

- Arquivo: `%LOCALAPPDATA%\\Atualizador.Windows\\history.json`.
- Formato JSON versionado, versão `1`.
- Retenção fixa documentada de no máximo 100 registros, com os mais recentes primeiro.
- Escrita em arquivo temporário e substituição do destino para reduzir risco de arquivo parcial.
- stdout e stderr integrais não são persistidos.

### Verificações

- `dotnet build Atualizador.Windows.slnx --configuration Release --no-restore`: 0 avisos e 0 erros.
- `dotnet test tests/Atualizador.Windows.Tests/Atualizador.Windows.Tests.csproj --configuration Release --no-build`: 16 testes aprovados, 0 falhas.
- Testes de armazenamento cobrem leitura/gravação, corrupção, retenção e limpeza.
- Descoberta real do WinGet foi persistida e exibida na tela `Histórico`, incluindo o detalhe de `Microsoft Edge`.
- Limpeza é protegida por confirmação e não foi acionada durante a validação para preservar os registros locais.
