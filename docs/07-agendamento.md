# Etapa 7 — Agendamento automático

**Estado inicial:** PENDENTE  
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

Registrar API/biblioteca escolhida, permissões solicitadas, teste de criar/executar/pausar/remover e limitações do ambiente.
