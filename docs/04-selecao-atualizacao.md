# Etapa 4 — Lista e seleção de aplicativos

**Estado inicial:** PENDENTE  
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

Registrar teste manual da lista, navegação por teclado e resultados do build.
