# Instruções permanentes para o Codex

## Escopo

- Leia `README.md`, `CLAUDE.md`, `docs/00-plano-geral.md` e o arquivo da etapa solicitada antes de alterar o projeto.
- Execute somente a etapa solicitada. Não antecipe WinGet, atualização de aplicativos, agendamento, serviço Windows ou publicação.
- Preserve `CLAUDE.md` para uso futuro no Claude Code. Estas instruções adaptam o fluxo para o Codex e não dependem de recursos do Claude.

## Desenvolvimento

- Use C#, WPF, XAML e .NET 10 LTS com alvo `net10.0-windows` quando o SDK estiver disponível.
- Prefira MVVM leve e mantenha regras de negócio fora do code-behind.
- Crie pastas e abstrações somente quando houver responsabilidade real.
- Centralize recursos visuais em dicionários XAML. Não adicione bibliotecas sem necessidade demonstrada.
- Preserve alterações preexistentes. Não use comandos destrutivos para limpar ou restaurar o repositório.

## Verificação

- Inspecione SDK, solução, projetos e estado do Git antes de implementar.
- Execute `dotnet build` e os testes aplicáveis após alterações de código.
- Registre somente resultados realmente verificados em `docs/00-plano-geral.md` e no documento da etapa.
- Faça uma revisão de simplicidade, responsabilidades, nullable reference types, limites de arquivo e avisos do compilador.
- Registre limitações de ambiente sem inventar resultados.

## Segurança e escopo futuro

- Não execute `winget upgrade` durante desenvolvimento ou testes.
- Não execute shell concatenando entrada de usuário.
- Não adicione banco de dados, serviço Windows, agendamento, inicialização automática ou integração externa antes da etapa correspondente.
- Escreva documentação e mensagens em português brasileiro, com termos consistentes e instruções verificáveis.
