# Etapa 3 — Descobrir atualizações do WinGet

**Estado:** CONCLUÍDA
**Modelo recomendado:** Sonnet 5.5, esforço médio; Opus 5.5/alto se o formato da saída for ambíguo.

## Objetivo

Verificar a disponibilidade do WinGet e listar pacotes com atualizações, sem instalar nada.

## Passos

1. Ler a skill `winget-safe-execution` e consultar a documentação oficial atual dos comandos `upgrade`, `list` e `source`.
2. Criar um serviço pequeno e testável responsável por localizar e executar o WinGet.
3. Verificar `winget --version` e `winget source list`. Exibir instruções claras se o WinGet não existir ou não puder iniciar.
4. Atualizar fontes somente por ação explícita do usuário ou por uma configuração documentada. Não executar `source reset` nem alterar fontes automaticamente.
5. Descobrir os pacotes com atualização disponível e associar cada registro a nome, ID, versão instalada, versão disponível, origem e indicador de versão desconhecida, quando esses dados estiverem disponíveis.
6. Confirmar se a versão instalada do WinGet fornece saída estruturada suportada para o comando escolhido. Não presumir que `upgrade --output json` existe. Se for necessário interpretar uma tabela, encapsular parsing e validar o comportamento em diferentes idiomas e exemplos reais antes de usar em produção.
7. Diferenciar falha de execução, falha de fonte, nenhuma atualização e resultado parcial.
8. Criar testes unitários com saídas fictícias para vários formatos, campos ausentes, espaços, identificadores com pontuação, cabeçalhos localizados e erros.
9. Usar uma interface de processo injetável ou equivalente simples para que os testes nunca executem o WinGet real.

## Segurança

- Não executar instaladores nesta etapa.
- Não inferir ID a partir do nome do aplicativo.
- Não confiar em IDs inventados por dados da interface.
- Capturar `stdout`, `stderr`, código de saída, timeout e cancelamento.
- Não registrar saídas integrais em logs permanentes sem avaliar se contêm caminhos ou dados pessoais.

## Critérios de aceite

- Ausência do WinGet é tratada sem encerrar abruptamente a aplicação.
- A descoberta não instala nem atualiza pacotes.
- Cada atualização é identificada por ID e origem verificáveis.
- Erros, resultados vazios e resultados parciais têm estados distintos.
- Testes de parsing não iniciam processos reais.

## Resultado da execução

### Arquivos alterados

- `src/Atualizador.Windows/Models/UpdatablePackage.cs`, `WingetSource.cs` e `WingetDiscoveryResult.cs`.
- `src/Atualizador.Windows/Services/ProcessExecution.cs`, `WingetLocator.cs`, `WingetOutputParser.cs`, `WingetSourceParser.cs` e `WingetService.cs`.
- `tests/Atualizador.Windows.Tests/Atualizador.Windows.Tests.csproj` e `tests/Atualizador.Windows.Tests/WingetTests.cs`.
- `Atualizador.Windows.slnx`, `docs/00-plano-geral.md` e este arquivo.

### Evidências reais

- `winget --version`: `v1.29.380`.
- `winget source list --disable-interactivity`: fontes `msstore`, `winget` e `winget-font`; `winget-font` estava explícita.
- `winget list --upgrade-available --disable-interactivity`: encontrou `Microsoft.Edge`, ID `Microsoft.Edge`, versão instalada `155.0.4283.45`, disponível `156.0.4314.42`, origem `winget`.
- Nenhum comando de instalação, atualização, `source update` ou `source reset` foi executado.

### Verificações

- `dotnet build Atualizador.Windows.slnx --configuration Release --no-restore`: 0 avisos e 0 erros.
- `dotnet test tests/Atualizador.Windows.Tests/Atualizador.Windows.Tests.csproj --configuration Release --no-build`: 9 testes aprovados, 0 falhas.
- Os testes usam `IProcessExecutor` e `IExecutableLocator` falsos; nenhum teste iniciou o WinGet real.

### Decisões e limitações

- A descoberta usa `winget list --upgrade-available --disable-interactivity`; `--include-unknown` só é acrescentado quando solicitado explicitamente.
- O parser usa posições do cabeçalho e cobre formatos inglês/português/espanhol nos testes. Saídas não reconhecidas retornam aviso e estado parcial, sem inventar ID ou origem.
- O serviço captura saída, erro, código de saída, cancelamento e tempo limite. O texto integral do processo não é persistido em logs.
