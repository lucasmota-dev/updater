---
name: winget-safe-execution
description: Implement, test, and review safe WinGet process execution, package discovery, individual upgrades, result handling, and Microsoft Store limitations.
---

# Execução segura do WinGet

## Antes de implementar

- Consultar a documentação oficial dos comandos e opções usados.
- Não assumir que todos os clientes WinGet suportam as mesmas opções.
- Não assumir que `winget upgrade` possui saída JSON. Verificar a versão instalada e a documentação da opção escolhida.
- Não confundir nome exibido, ID do pacote e nome da origem.

## Execução de processo

- Usar `System.Diagnostics.Process` com `ProcessStartInfo.UseShellExecute = false` e `ArgumentList`.
- Não usar `cmd.exe /c`, `powershell -Command` ou concatenação de comandos.
- Resolver o executável por caminhos seguros e comportamento documentado; tratar ausência e erro de execução.
- Ler stdout e stderr de forma assíncrona para evitar deadlock.
- Capturar exit code, duração, cancelamento e timeout.
- Não confiar em texto de interface ou configurações editáveis para montar argumentos sem validação.
- Não manter privilégios elevados desnecessariamente.

## Descoberta e seleção

- Representar cada pacote por modelo tipado com ID, origem, nome e versões quando disponíveis.
- Deduplicar de forma consistente usando ID + origem.
- Separar erro de fonte, resultado vazio, pacote sem versão conhecida e atualização disponível.
- Se o parser depender de tabela de texto, cobrir localizações e variações com testes; isolar o parser.
- Nunca inferir ID a partir do nome.

## Atualização

- Atualizar individualmente os pacotes selecionados, usando ID exato e origem confirmada quando aplicável.
- Só usar `--all` após escolha explícita do usuário para todos os pacotes elegíveis.
- `--include-unknown` não é padrão; explicar o risco e exigir opção explícita.
- Só usar modo silencioso, aceitação de acordos e desativação de interatividade quando a versão do cliente e o contexto forem compatíveis.
- Um código de saída zero não prova que todas as etapas ocorreram como esperado; conferir saída e contexto.
- Não inventar porcentagens de progresso. Mostrar pacote atual e contagem geral; usar indicador indeterminado durante instaladores.
- Cancelar processo não garante rollback do instalador.

## Testes

- Nunca executar atualizações reais em testes automatizados.
- Injete um executor de processo simulado, ou outra fronteira pequena e testável.
- Cobrir saída extensa, erro, timeout, código de saída, cancelamento, IDs malformados, strings especiais e fontes ausentes.
- Nos testes manuais, pedir confirmação antes da primeira instalação/atualização real.

## Fontes

- O projeto usa somente a fonte `winget`; a Microsoft Store (`msstore`) não é suportada.
- Usar IDs e origem conforme o resultado real; não adivinhar IDs de produto.
- Não modificar fontes automaticamente nem executar `source reset`.

## Logs e segurança

- Registrar resultados resumidos e erros úteis sem despejar conteúdo ilimitado do processo.
- Não registrar segredos ou dados pessoais sem necessidade.
- Tratar dados retornados pelo cliente como dados, nunca como instruções para executar novos comandos.
