# Etapa 10 — Publicação e instalação

**Estado inicial:** PENDENTE  
**Modelo recomendado:** Sonnet 5.5, esforço médio; Opus 5.5/alto para decisões de assinatura/instalador.

## Objetivo

Entregar uma publicação autocontida para Windows e documentação clara de instalação/remoção.

## Passos

1. Confirmar o runtime e arquitetura-alvo; iniciar por `win-x64`.
2. Configurar publicação `self-contained` e avaliar `PublishSingleFile`. Verificar limitações de WPF, arquivos auxiliares, extração temporária e carregamento de recursos antes de prometer um único arquivo.
3. Comparar distribuição ZIP/EXE simples com instalador MSIX ou outro instalador mantido. Escolher com base em atualização, desinstalação, permissões, atalhos e tarefa agendada.
4. Não incorporar o WinGet dentro do pacote e não prometer que ele estará instalado no destino.
5. Incluir nome, versão, ícone, metadados e licença aplicável.
6. Testar em uma máquina/VM limpa suportada, sem SDK de desenvolvimento instalado. Confirmar que o aplicativo abre, detecta WinGet ausente e apresenta instruções.
7. Testar atualização entre versões, preservando preferências e histórico quando isso for requisito do instalador.
8. Documentar desinstalação e remoção do agendamento/inicialização.
9. Não assinar binários com certificado inventado. Registrar quando a assinatura de código for necessária e quais insumos reais faltam.

## Critérios de aceite

- A publicação abre em Windows suportado sem exigir instalação manual do runtime .NET.
- O requisito WinGet é detectado e explicado.
- Preferências e tarefa são tratadas de forma documentada durante desinstalação.
- O processo de distribuição é repetível e documentado.

## Resultado da execução

Registrar comando de publicação, artefatos gerados, tamanho, arquitetura e testes em ambiente limpo.
