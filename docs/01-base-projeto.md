# Etapa 1 — Base do projeto

**Estado inicial:** PENDENTE  
**Modelo recomendado:** Sonnet 5.5, esforço médio. Use Opus 5.5/alto se houver decisão arquitetural difícil.

## Objetivo

Criar a solução WPF mínima, compilável e pronta para evoluir. Não implementar atualização do WinGet nesta etapa.

## Passos

1. Inspecionar a pasta, o estado Git e as ferramentas instaladas. Preservar arquivos existentes.
2. Confirmar a versão do SDK .NET com `dotnet --info` e `dotnet --list-sdks`.
3. Consultar a documentação oficial e usar `net10.0-windows` se o SDK correspondente estiver instalado. Não instalar SDK globalmente sem autorização. Se não estiver disponível, documentar o bloqueio e escolher a alternativa suportada mais próxima apenas para validar a base.
4. Criar uma solução e um projeto WPF com nomes consistentes, por exemplo `Atualizador.Windows.sln` e `src/Atualizador.Windows/Atualizador.Windows.csproj`.
5. Criar a janela principal com título, tamanho inicial adequado e conteúdo de boas-vindas simples.
6. Criar a estrutura mínima para `Models`, `ViewModels`, `Views`, `Services` e `Resources` somente quando cada pasta tiver uso real. Não criar pastas vazias desnecessárias.
7. Configurar nullable reference types e análise básica do compilador sem adicionar regras que bloqueiem desnecessariamente a primeira compilação.
8. Adicionar um ponto de entrada e um ViewModel mínimo para confirmar binding.
9. Definir nome provisório do aplicativo como `Atualizador Windows`; não criar logotipo complexo nesta etapa.
10. Executar `dotnet build` e corrigir erros.

## Restrições

- Não adicionar banco de dados.
- Não adicionar bibliotecas de terceiros sem necessidade demonstrada.
- Não executar `winget upgrade`.
- Não adicionar serviço Windows, agendamento ou inicialização automática.
- Não construir toda a arquitetura antes de haver necessidade real.

## Critérios de aceite

- A solução compila.
- A janela abre em Windows com conteúdo inicial.
- Existe uma separação mínima entre janela e estado exibido.
- O projeto usa o alvo .NET documentado, ou a limitação do SDK está registrada.
- Nenhuma atualização de aplicativo foi executada.

## Verificações

Executar `dotnet build <caminho-da-solução>` e inspecionar erros/avisos. Testar abrir a janela se o ambiente for Windows.

## Resultado da execução

Preencher após executar: arquivos alterados, SDK encontrado, comandos executados, resultado real e limitações. Não marcar como concluída antecipadamente.
