# Plano geral

## Objetivo

Construir o aplicativo por etapas pequenas e verificáveis. Implementar apenas uma etapa por solicitação. Atualizar estado, decisões e bloqueios ao finalizar cada etapa.

## Stack aprovada

- C# / WPF / XAML.
- .NET 10 LTS, alvo `net10.0-windows`, se disponível no ambiente.
- MVVM leve.
- WinGet como mecanismo principal de detecção e atualização.
- Agendador de Tarefas do Windows; sem serviço próprio na primeira versão.
- Publicação autocontida `win-x64` como primeiro alvo.

## Etapas e estado

| Etapa | Arquivo | Estado | Depende de |
|---|---|---|---|
| 1 | `01-base-projeto.md` | CONCLUÍDA | — |
| 2 | `02-interface-temas.md` | CONCLUÍDA | 1 |
| 3 | `03-descoberta-updates.md` | CONCLUÍDA | 1 |
| 4 | `04-selecao-atualizacao.md` | CONCLUÍDA | 2, 3 |
| 5 | `05-execucao-progresso.md` | CONCLUÍDA | 4 |
| 6 | `06-historico-logs.md` | CONCLUÍDA | 5 |
| 7 | `07-agendamento.md` | CONCLUÍDA | 5, 6 |
| 8 | `08-msstore.md` | CONCLUÍDA | 3, 5 |
| 9 | `09-notificacoes-inicializacao.md` | CONCLUÍDA | 7, 8 |
| 10 | `10-publicacao-instalacao.md` | CONCLUÍDA | 1–9 |
| 11 | `11-testes-seguranca.md` | CONCLUÍDA | 1–10 |
| 12 | `12-revisao-final.md` | CONCLUÍDA | 1–11 |

Os estados acima representam o início do trabalho. Não marcar etapas como concluídas antes da execução real.

## Ordem recomendada

Executar de 1 a 6 primeiro. Depois, abordar agendamento e Microsoft Store em etapas separadas. Concluir notificações, publicação e revisão no fim. Se uma etapa depender de informação não confirmada, registrar `BLOQUEADA` e continuar apenas tarefas independentes autorizadas.

## Regras de conclusão

Para concluir uma etapa:

1. Implementar somente o escopo definido no arquivo.
2. Atender aos critérios de aceite.
3. Executar as verificações que o ambiente permitir.
4. Registrar comandos e resultados reais na seção `Resultado da execução` da etapa.
5. Executar a revisão de qualidade para mudanças de código.
6. Atualizar esta tabela.
7. Listar limitações e testes que exigem Windows físico ou conta Store.

## Decisões em aberto

Registrar aqui decisões que exigem pesquisa ou validação. Não presumir que um comando do WinGet, API experimental do WPF ou pacote NuGet esteja disponível sem confirmação.

- Biblioteca/estratégia de estilo WPF: decidir durante a etapa 2 após verificar estado e compatibilidade atuais.
- Formato estruturado da saída do WinGet: verificar durante a etapa 3; não assumir JSON.
- API/biblioteca para Agendador de Tarefas: comparar durante a etapa 7, priorizando a solução simples e segura.
- Alcance real da fonte `msstore`: validar na etapa 8 sem prometer cobertura total.

## Melhorias futuras planejadas

Os itens abaixo foram solicitados e já têm implementação parcial no checkout; as validações externas indicadas permanecem pendentes:

- Bandeja do Windows: implementada; falta validação manual completa da interação em sessão interativa.
- Instalador Inno Setup: script implementado; falta compilar e testar o instalador em ambiente com Inno Setup.
- Ícone Material Symbols: SVG, licença e `.ico` 16/32/48/256 implementados.
- Publicação contínua no GitHub: workflow implementado; falta executar no repositório remoto e confirmar a release/tag contínua.

## Registro de alterações

- Inicial: pacote de instruções e skills criado. Nenhuma aplicação WPF foi implementada nesta entrega.
- Etapa 1: solução WPF mínima criada e validada com SDK .NET `10.0.402`. A janela foi aberta no Windows e encerrada após a verificação. Não há bloqueios conhecidos para esta etapa.
- Etapa 2: interface WPF com navegação, temas Sistema/Claro/Escuro, destaques configuráveis e persistência em `%LOCALAPPDATA%` validada no Windows. Não há bloqueios conhecidos para esta etapa.
- Etapa 3: serviço seguro de descoberta do WinGet, parser isolado, estados de erro/parcial/vazio e nove testes automatizados implementados. WinGet `v1.29.380` foi verificado sem executar atualizações.
- Etapa 4: lista virtualizável com seleção tipada, filtros por origem, estados vazio/erro, seleção preservada e atualização desabilitada até a etapa 5. Fluxo real validado com `Microsoft.Edge` sem instalar.
- Etapa 5: atualização individual sequencial com argumentos seguros, confirmação, progresso por pacote, cancelamento, estados por item e 12 testes automatizados simulados. Nenhuma atualização real foi executada.
- Etapa 6: histórico JSON versionado com escrita atômica, retenção de 100 registros, recuperação de corrupção, filtros, detalhes, abertura de pasta e limpeza confirmada. Descoberta real persistida e exibida.
- Etapa 7: configuração e persistência de agendamento, tarefa nativa com nome fixo, escopo explícito, modo `--scheduled`, exclusão mútua e testes reais de criar/desativar/reativar/remover sem disparar atualização.
- Etapa 8: origem `msstore` preservada na identidade dos pacotes, deduplicação por ID+origem e botão para a página oficial de atualizações da Microsoft Store.
- Etapa 9: notificações internas não bloqueantes, opção explícita de inicialização e tarefa removível, desligada por padrão. Não foi criado serviço Windows.
- Etapa 10: publicação autocontida `win-x64` em arquivo único, ZIP repetível e validação de abertura; WinGet continua sendo requisito externo.
- Etapa 11: conjunto de 21 testes automatizados aprovado, revisão de chamadas de processo/argumentos/caminhos e validações reais sem executar atualização de software.
- Etapa 12: revisão final concluída; todas as etapas estão documentadas, build/testes/publicação foram repetidos e as limitações conhecidas foram registradas.
- Melhorias posteriores: bandeja, ícone Material Symbols, script Inno Setup e workflow de release contínuo adicionados; compilação do instalador e execução do workflow remoto permanecem pendentes de ambiente externo.
- Próximos recursos implementados:
  - Estilização completa de botões (Primary e Secondary) com referência visual no WinHawk / Windows 11 Fluent (`CornerRadius`, templates explícitos e estados de hover/press/disabled com tokens compartilhados), suporte à cor de destaque do sistema e anel de foco acessível.
  - Exclusão de aplicativos da atualização: exclusão direta na tela de Atualizações, persistência atômica em `%LOCALAPPDATA%\Atualizador.Windows\excluded-packages.json`, tela de gerenciamento exclusivo em `Configurações > Apps excluídos da atualização` e bloqueio automático de pacotes excluídos em tarefas agendadas e atualizações manuais.
  - Instalação de aplicativos pelo WinGet: nova página `Instalar`, pesquisa no catálogo `winget`/`msstore`, identificação clara de publicador e origem, avisos de segurança, seleção múltipla com barra de progresso, cancelamento seguro, tratamento de elevação administrativa/interação e registro no histórico como `Instalacao`.
  - Suíte de testes automatizados expandida para 34 testes com 100% de aprovação.
