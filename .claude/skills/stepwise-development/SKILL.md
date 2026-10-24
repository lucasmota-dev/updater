---
name: stepwise-development
description: Run the project in small documented stages, executing only the requested step and recording verified outcomes before advancing.
---

# Desenvolvimento por etapas

1. Leia `CLAUDE.md`, `README.md`, `docs/00-plano-geral.md` e o arquivo da etapa solicitada.
2. Inspecione o estado atual antes de editar.
3. Execute apenas a etapa solicitada. Não antecipe funcionalidades de etapas seguintes.
4. Preserve alterações anteriores e evite operações destrutivas.
5. Implemente os critérios de aceite, não funcionalidades extras.
6. Execute build/testes pertinentes. Não simule resultados como se fossem reais.
7. Aplique revisão de qualidade após alterações de código.
8. Atualize `Resultado da execução` com arquivos, comandos, resultados e limitações reais.
9. Atualize o estado no plano geral. Use somente `PENDENTE`, `EM ANDAMENTO`, `BLOQUEADA` e `CONCLUÍDA`.
10. Não marque conclusão se houver critérios não verificados.
11. Pare e informe o resultado. Aguarde a próxima instrução antes de iniciar a próxima etapa.
12. Se um detalhe for desconhecido, pesquise a documentação oficial atual. Não invente APIs, argumentos, comportamento ou resultados.
