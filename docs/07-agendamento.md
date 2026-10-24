# Etapa 7 — Agendamento automático

**Estado:** CONCLUÍDA
**Modelo recomendado:** Opus 5.5, esforço alto por envolver identidade e execução em segundo plano.

## Objetivo

Permitir agendamentos periódicos sem manter um serviço Windows próprio em execução.

## Passos

1. Confirmar a documentação atual do Agendador de Tarefas e avaliar opções de API. Comparar API nativa/COM e biblioteca NuGet mantida. Usar a alternativa mais simples e auditável.
2. Documentar o contexto de execução: conta do usuário, sessão conectada, permissões e limitações de aplicativos da Store. Não prometer operação universal com usuário desconectado.
3. Criar configurações para desligado, intervalo em horas, diário e semanal; definir e validar limites razoáveis.
4. Criar uma tarefa com nome/versionamento fixo, sem exigir elevação para executar o aplicativo. A criação/alteração pode exigir consentimento conforme a opção escolhida; explicar antes.
5. A tarefa deve executar somente uma opção não interativa do próprio aplicativo ou um componente dedicado e controlado. Não passar comandos arbitrários do usuário.
6. Implementar modo de execução agendada sem janela principal, com saída e códigos de status registráveis.
7. Garantir que a tarefa atualize somente o escopo configurado: pacotes selecionados ou todos os elegíveis, escolhido explicitamente.
8. Evitar nova execução enquanto outra atualização estiver ativa; usar mecanismo simples de exclusão mútua.
9. Implementar criar, atualizar, desativar, reativar e remover a tarefa, validando o nome e a propriedade antes de alterar.
10. Testar agendamento com modo de simulação que não instala pacotes. Só testar atualização real manualmente e com autorização.
11. Explicar que processos podem exigir UAC, que alguns instaladores ignoram modo silencioso e que sessão desconectada pode impedir WinGet/Store.
12. Não implementar serviço Windows nesta etapa. Reavaliar isso apenas se uma necessidade concreta surgir.

## Critérios de aceite

- Usuário define, salva, pausa e remove o agendamento.
- Nenhum comando arbitrário é executado pela tarefa.
- A tarefa respeita a opção de seleção e gera histórico.
- Uma execução não duplica outra já ativa.
- Limitações de sessão, privilégios e Store são documentadas.

## Resultado da execução

### Decisão e implementação

- Foi escolhido `schtasks.exe`, ferramenta nativa documentada pela Microsoft, em vez de adicionar biblioteca NuGet ou interop COM.
- A tarefa usa o nome fixo `\\Atualizador.Windows.AutoUpdate.v1`, ação fixa do executável atual com `--scheduled`, `/IT` e `/RL LIMITED`.
- A configuração é salva em `%LOCALAPPDATA%\\Atualizador.Windows\\schedule.json`.
- Modos disponíveis: desligado, a cada hora, diário e semanal. O intervalo horário é limitado a 1–168 horas.
- O escopo é explícito: todos os elegíveis ou pacotes selecionados persistidos.
- O modo agendado usa um mutex para evitar duas execuções simultâneas.

### Verificações

- `dotnet build Atualizador.Windows.slnx --configuration Release --no-restore`: 0 avisos e 0 erros.
- `dotnet test tests/Atualizador.Windows.Tests/Atualizador.Windows.Tests.csproj --configuration Release --no-build`: 21 testes aprovados, 0 falhas.
- Testes simulados verificam persistência, nome fixo e argumentos sem comandos arbitrários.
- Teste real limitado do Windows Task Scheduler: tarefa temporária criada, desativada, reativada e removida com sucesso. A tarefa não foi executada.

### Limitações

- A tarefa roda na conta e no contexto de sessão do usuário atual. WinGet e Microsoft Store podem não funcionar com sessão desconectada, sem acordos aceitos ou quando um instalador exige interação/UAC.
- O aplicativo não cria serviço Windows e não promete execução universal em segundo plano.
