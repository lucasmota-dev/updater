# Instruções permanentes do projeto

Leia este arquivo e `README.md` antes de implementar. Consulte `docs/00-plano-geral.md` para saber a etapa ativa.

## Objetivo e escopo

Criar um atualizador desktop Windows usando C#, WPF e .NET 10 LTS. O aplicativo deve ser simples, seguro, acessível e fácil de manter. Desenvolver em etapas; não antecipar funcionalidades de etapas futuras.

## Fluxo obrigatório

1. Ler a etapa solicitada por completo e suas dependências.
2. Inspecionar a estrutura existente, o SDK, os projetos, os testes e o estado do Git.
3. Preservar alterações preexistentes. Não descartar arquivos nem executar comandos destrutivos.
4. Implementar somente o escopo da etapa atual.
5. Executar build, testes e verificações aplicáveis. Não afirmar que um teste passou se ele não foi executado.
6. Aplicar `thermo-nuclear-code-quality-review` após alterações de código.
7. Atualizar o checklist de `docs/00-plano-geral.md` e o próprio arquivo da etapa com resultado real.
8. Informar arquivos alterados, comandos executados, resultado e limitações. Parar antes da próxima etapa.

Estados permitidos: `PENDENTE`, `EM ANDAMENTO`, `BLOQUEADA`, `CONCLUÍDA`. Só usar `CONCLUÍDA` quando todos os critérios de aceite forem verificados.

## Skills obrigatórias conforme a tarefa

- Criação e refatoração: `.claude/skills/thermo-nuclear-code-generation/SKILL.md`.
- Revisão de código: `.claude/skills/thermo-nuclear-code-quality-review/SKILL.md`.
- Textos e documentação em português: `.claude/skills/pt-br-linguagem-tecnica-controlada/SKILL.md`.
- Interface, cores e temas: `.claude/skills/windows-11-wpf-design/SKILL.md`.
- Execução segura do WinGet: `.claude/skills/winget-safe-execution/SKILL.md`.
- Organização por etapas: `.claude/skills/stepwise-development/SKILL.md`.
- WPF/MVVM: `.claude/skills/wpf-development/SKILL.md`.

Leia skills relevantes antes da tarefa. Não afirme ter usado skills ou agentes que não foram acessados. Use agentes especializados quando isso reduzir trabalho ou melhorar uma revisão independente; não delegue tarefas triviais sem necessidade.

## Stack e arquitetura

- C# e WPF com XAML.
- Alvo preferencial: `net10.0-windows`; confirmar o SDK instalado e consultar a documentação oficial antes de fixar versão ou pacote.
- MVVM leve, com modelos tipados, comandos e binding. Evitar code-behind com regras de negócio.
- Separar UI, serviços do sistema, modelos e persistência apenas quando houver responsabilidade real. Não criar camadas ou interfaces sem benefício concreto.
- Preferir APIs oficiais e dependências pequenas. Antes de instalar uma biblioteca de aparência ou agendamento, verificar manutenção, licenciamento, compatibilidade e necessidade.
- Não introduzir serviço Windows próprio na primeira versão. Usar Agendador de Tarefas quando a etapa de agendamento for aprovada.
- Não ultrapassar 1.000 linhas por arquivo sem justificativa excepcional; decompor quando isso melhorar coesão.

## Regras de segurança

- Iniciar o aplicativo sem elevação administrativa.
- Nunca executar shell concatenando entrada do usuário. Usar `ProcessStartInfo.ArgumentList`, argumentos explícitos e `UseShellExecute = false`.
- Nunca confiar apenas no nome mostrado na interface. Executar pelo identificador e pela origem obtidos do WinGet e validados.
- Não aceitar argumentos arbitrários, scripts, caminhos ou comandos fornecidos por uma linha de configuração.
- Não atualizar nem instalar aplicativos em testes automatizados. Criar testes com serviço simulado e dados fictícios.
- Não usar `--all` para executar uma seleção individual. Só atualizar tudo quando o usuário escolher essa opção explicitamente.
- Não habilitar `--include-unknown` silenciosamente. Deve ser uma opção explícita e explicada.
- Não alterar fontes do WinGet, políticas, UAC, registro, firewall ou configurações globais sem pedido explícito.
- Não registrar tokens, dados sensíveis ou conteúdo integral desnecessário dos processos. Sanitizar logs.
- Cancelar um processo não garante que um instalador externo reverta alterações. Exibir essa limitação.
- Prevenir duas atualizações concorrentes e evitar que o agendamento inicie outra execução já ativa.

## Interface e experiência

Siga `.claude/skills/windows-11-wpf-design/SKILL.md`. Interface em português brasileiro, estilo Windows 11, visual moderno e contido. Oferecer tema Sistema/Claro/Escuro e cor de destaque do sistema, paleta predefinida e cor personalizada validada. Garantir contraste, foco visível, navegação por teclado, estados de carregamento, vazio, erro e sucesso. Persistir preferências por usuário.

Não usar imagens remotas como dependência dos ícones. Preferir ícones locais e fallback consistente. Não imitar elementos do Windows sem necessidade. Efeitos translúcidos devem ser moderados e não comprometer contraste/desempenho.

## WinGet

- O aplicativo usa apenas a fonte `winget`. Não há suporte à fonte `msstore`/Microsoft Store e a descoberta de atualizações consulta somente `--source winget`.

- Verificar o comportamento real da versão instalada e a documentação oficial antes de depender de uma opção de CLI.
- Não presumir que a saída do WinGet é JSON nem que todas as versões oferecem saída estruturada para `upgrade`. Validar abordagem de descoberta e parsing com versões reais e saída de diferentes idiomas.
- Usar `--id`, `--exact`, `--source` e opções silenciosas explícitas quando forem aplicáveis e confirmadas pela documentação. Não inventar parâmetros.
- Capturar `stdout`, `stderr`, código de saída, cancelamento e timeout. Tratar códigos não zero sem encobrir o erro.
- Exibir progresso por pacote. Usar barra indeterminada durante tarefas sem percentual confiável.

## Versionamento e release

- Versão no padrão `yyyy.MM.dd.NN` (ex.: `2026.10.28.01`; `NN` reinicia a cada dia, horário de Brasília). Tag Git: `v2026.10.28.01`.
- A versão nunca é escrita à mão: `tools/Get-NextVersion.ps1` a calcula e o workflow `release.yml` a injeta (`-p:Version`, `/DAppVersion`). Compilações locais ficam em `0.0.0.0` e não verificam atualização.
- Release só pelo workflow `Release` (manual, branch `main`). `ci.yml` valida PRs, pushes no `main` e roda semanalmente.

## Testes e qualidade

- Não executar testes que modifiquem o sistema do desenvolvedor.
- Preferir abstrações de processo testáveis e executáveis falsos/simulados.
- Testar parsing da saída, IDs, origens, seleção, cancelamento, erro, persistência de tema/cor e criação/remoção de tarefa com camada isolada.
- Verificar build e testes ao final de cada etapa. Se o ambiente não tiver Windows, informar quais testes WPF ou integração real não puderam ser executados.
- Revisar simplificação estrutural, responsabilidades, complexidade, tipos, limites de arquivo e tratamento de erros.

## Comunicação

Escreva em português brasileiro, com instruções diretas e terminologia consistente. Não faça perguntas cuja resposta possa ser obtida inspecionando o repositório ou a documentação oficial. Se faltar uma decisão realmente indispensável, registre o bloqueio e continue o trabalho independente.

## Modelo e esforço recomendados

- Implementação delimitada: Claude Sonnet 5.5, esforço médio.
- Arquitetura complexa, segurança ou revisão final: Claude Opus 5.5, esforço alto.
- Tarefas pequenas de apoio: Claude Haiku 5.5, quando disponível.

A disponibilidade depende da conta e da versão do Claude Code. Não altere configurações do usuário automaticamente.
