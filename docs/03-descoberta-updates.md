# Etapa 3 — Descobrir atualizações do WinGet

**Estado inicial:** PENDENTE  
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

Registrar formato de saída observado, versão de WinGet testada, fontes presentes, testes e limitações. Não declarar suporte geral sem evidência.
