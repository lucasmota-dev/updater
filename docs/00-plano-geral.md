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
| 1 | `01-base-projeto.md` | PENDENTE | — |
| 2 | `02-interface-temas.md` | PENDENTE | 1 |
| 3 | `03-descoberta-updates.md` | PENDENTE | 1 |
| 4 | `04-selecao-atualizacao.md` | PENDENTE | 2, 3 |
| 5 | `05-execucao-progresso.md` | PENDENTE | 4 |
| 6 | `06-historico-logs.md` | PENDENTE | 5 |
| 7 | `07-agendamento.md` | PENDENTE | 5, 6 |
| 8 | `08-msstore.md` | PENDENTE | 3, 5 |
| 9 | `09-notificacoes-inicializacao.md` | PENDENTE | 7, 8 |
| 10 | `10-publicacao-instalacao.md` | PENDENTE | 1–9 |
| 11 | `11-testes-seguranca.md` | PENDENTE | 1–10 |
| 12 | `12-revisao-final.md` | PENDENTE | 1–11 |

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

## Registro de alterações

- Inicial: pacote de instruções e skills criado. Nenhuma aplicação WPF foi implementada nesta entrega.
