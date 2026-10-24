# Etapa 10 — Publicação e instalação

**Estado:** CONCLUÍDA
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

### Publicação escolhida

- Alvo: Windows x64 (`win-x64`).
- Distribuição: ZIP simples contendo o executável autocontido em arquivo único.
- Não foi incorporado WinGet ao pacote. O aplicativo detecta o cliente instalado e explica quando ele está ausente.
- A tarefa agendada e a inicialização usam caminhos fixos e podem ser removidas pela interface.

### Comandos e resultados

- `dotnet restore src/Atualizador.Windows/Atualizador.Windows.csproj --runtime win-x64`: concluído.
- `dotnet publish src/Atualizador.Windows/Atualizador.Windows.csproj --configuration Release --no-restore --output artifacts/publish/win-x64 -p:PublishProfile=WinX64`: concluído.
- Artefato: `artifacts/publish/win-x64/Atualizador.Windows.exe`, aproximadamente 140 MB.
- Pacote: `artifacts/Atualizador.Windows-0.1.0-win-x64.zip`, aproximadamente 59 MB.
- Executável publicado iniciado no Windows e encerrado após a verificação.

### Limitações

- A publicação não foi testada em uma VM limpa sem SDK neste ambiente; a validação executada foi no Windows de desenvolvimento.
- O binário usa o ícone padrão do WPF nesta versão; não há logotipo personalizado.
- A assinatura de código não foi aplicada porque não existe certificado fornecido.
- O WinGet deve estar instalado no computador de destino para descoberta e atualização.

## Melhorias executadas — instalador, ícone e publicação contínua

**Estado:** EM ANDAMENTO

### Instalador Inno Setup

- Gerar `Atualizador-Setup.exe` para Windows x64 com Inno Setup.
- Instalar em `C:\Atualizador`.
- Instalar o executável portátil e `README.txt`.
- Criar atalhos na Área de Trabalho e no Menu Iniciar.
- Incluir desinstalador e remover atalhos/arquivos instalados sem remover dados do usuário em `%LOCALAPPDATA%`.
- Solicitar privilégios somente quando a instalação escolhida exigir; avaliar `PrivilegesRequired=lowest` e documentar a decisão.

### Ícone

- Obter um símbolo de atualização do sistema na fonte oficial Google Material Symbols.
- Manter SVG de origem e atribuição/licença Apache 2.0.
- Converter para `.ico` com 16, 32, 48 e 256 px.
- Aplicar o ícone ao executável, janela, atalhos e instalador.

### GitHub Actions

- Criar posteriormente `.github/workflows/release-continuous.yml` com `push` em `main` e `workflow_dispatch`.
- Usar `windows-latest`, restaurar, compilar, executar testes e somente depois gerar `Atualizador.exe` e `Atualizador-Setup.exe`.
- Usar `permissions: contents: write` e manter as demais permissões como `none`.
- Usar `concurrency` com `cancel-in-progress: true` para impedir que uma publicação antiga substitua uma mais nova.
- Manter uma release contínua com tag fixa, por exemplo `continuous`. Antes de publicar, atualizar/verificar a referência da tag para `GITHUB_SHA`; depois usar `gh release edit --target GITHUB_SHA` e conferir a referência via API do GitHub.
- Substituir os ativos com `gh release upload ... --clobber`, conferir nomes, tamanhos e SHA do commit, e falhar se a referência da tag não coincidir com `GITHUB_SHA`.
- Não assumir que substituir ativos atualiza a referência Git: a verificação explícita da tag é obrigatória.

Referências consultadas: [workflow syntax e permissões](https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-syntax), [gatilhos de workflow](https://docs.github.com/en/actions/how-tos/write-workflows/choose-when-workflows-run/trigger-a-workflow), [concorrência](https://docs.github.com/en/actions/concepts/workflows-and-actions/concurrency), [`gh release edit`](https://cli.github.com/manual/gh_release_edit) e [`gh release upload --clobber`](https://cli.github.com/manual/gh_release_upload).

Referências de distribuição: [guia oficial Material Symbols](https://developers.google.com/fonts/docs/material_symbols?hl=pt-br), [Inno Setup](https://jrsoftware.org/isinfo.php), [`PrivilegesRequired`](https://jrsoftware.org/ishelp/topic_setup_privilegesrequired.htm) e seção [`Icons`](https://jrsoftware.org/ishelp/topic_iconssection.htm).

Implementado neste checkout: SVG oficial, licença, `.ico`, `installer/Atualizador.iss` e `.github/workflows/release-continuous.yml`. Pendente de validação externa: compilação real do Inno Setup e execução do workflow no GitHub.

O executável portátil foi recompilado com o ícone local e o aplicativo Release iniciou no Windows. A compilação do instalador não foi declarada como concluída porque `ISCC.exe` não está disponível neste ambiente.

Nota de segurança da validação: o modo publicado `--scheduled` foi executado enquanto havia uma configuração de agendamento local ativa. Isso tentou atualizar o Edge, o WinGet retornou falha e a versão instalada permaneceu inalterada. Não executar esse modo durante testes sem garantir que o agendamento local esteja desligado.
