# Etapa 6 — Histórico e registros

**Estado inicial:** PENDENTE  
**Modelo recomendado:** Sonnet 5.5, esforço médio.

## Objetivo

Guardar histórico local compreensível das verificações e das atualizações.

## Passos

1. Definir um modelo simples de execução: data, início/fim, origem, quantidade, status geral e resultado por pacote.
2. Salvar em `%LOCALAPPDATA%\<NomeDoApp>\`, sem precisar de administrador.
3. Escolher JSON ou SQLite somente após avaliar volume e necessidade. Para o primeiro MVP, preferir JSON versionado se o histórico permanecer pequeno.
4. Escrever dados de forma resiliente para reduzir risco de arquivo parcial. Tratar arquivo corrompido sem travar a interface.
5. Criar tela de histórico com filtro por data/status, detalhes do pacote e mensagem resumida de erro.
6. Aplicar limite configurável ou política documentada de retenção. Não apagar registros inesperadamente.
7. Registrar exceções e metadados úteis, sem conteúdo ilimitado de stdout/stderr.
8. Oferecer ação para abrir a pasta de logs e limpar o histórico com confirmação.
9. Não incluir senhas, tokens, informações pessoais desnecessárias ou dados completos do ambiente.

## Critérios de aceite

- Histórico permanece após reiniciar a aplicação.
- Arquivo corrompido não impede abrir o app.
- Logs diferenciam erro de descoberta e erro de atualização.
- Limpeza exige confirmação e explica o alcance.

## Resultado da execução

Registrar formato adotado, local de armazenamento e testes de leitura/gravação/recuperação.
