# Etapa 11 — Testes e segurança

**Estado inicial:** PENDENTE  
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

Listar comando e resultado de cada teste realmente executado. Não classificar algo não testado como aprovado.
