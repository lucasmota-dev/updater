# Etapa 5 — Atualização, progresso e cancelamento

**Estado inicial:** PENDENTE  
**Modelo recomendado:** Opus 5.5, esforço alto para definir o fluxo seguro; Sonnet 5.5/medio para implementação após aprovação do desenho.

## Objetivo

Atualizar somente os pacotes selecionados e apresentar progresso, resultado e falhas por aplicativo.

## Passos

1. Fazer revisão do fluxo antes de habilitar atualizações reais.
2. Usar `ProcessStartInfo` com `UseShellExecute = false` e `ArgumentList`. Não construir uma linha de comando concatenando valores livres e não usar `cmd.exe /c`.
3. Montar argumentos a partir de um pacote tipado validado. Incluir `--id`, `--exact` e `--source` quando o comando e a origem justificarem. Verificar cada opção na documentação da versão alvo.
4. Configurar modo silencioso e aceitação de acordos somente com comportamento documentado. Não usar flags não confirmadas.
5. Atualizar sequencialmente para evitar conflitos entre instaladores. Reavaliar concorrência somente com motivo comprovado e testes.
6. Capturar `stdout` e `stderr` enquanto o processo roda para evitar bloqueio por buffer cheio.
7. Exibir pacote atual, estado textual, quantidade concluída e barra geral por pacotes processados.
8. Durante a instalação, usar progresso indeterminado caso não exista porcentagem confiável. Não transformar o tempo decorrido em porcentagem falsa.
9. Permitir cancelar o processo filho quando for seguro. Informar que encerrar o processo do WinGet não garante que o instalador externo reverta alterações.
10. Exibir estados finais por pacote: concluído, falhou, cancelado, ignorado ou reinicialização necessária, apenas quando esses estados forem detectáveis.
11. Tratar código de saída, timeout, elevação administrativa e instalador que abre uma interface apesar do modo silencioso.
12. Impedir duas atualizações simultâneas da mesma instância e coordenar com execução agendada.
13. Usar processo simulado nos testes. Exigir confirmação explícita antes do primeiro teste manual com atualizações reais.

## Regras de segurança

- Não executar `winget upgrade --all` para uma seleção parcial.
- Não incluir `--include-unknown` por padrão. Deve haver uma opção explícita, com explicação.
- Não reiniciar o Windows automaticamente.
- Não ocultar erros de fonte, acordo, permissão, rede ou instalador.

## Critérios de aceite

- Cada pacote selecionado produz resultado próprio.
- A barra geral acompanha a quantidade real concluída.
- A UI continua responsiva.
- Cancelamento, erro e código de saída são tratados.
- Testes automatizados não atualizam aplicativos reais.

## Resultado da execução

Registrar comandos confirmados, códigos de saída observados e testes reais/simulados claramente separados.
