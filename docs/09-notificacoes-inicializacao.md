# Etapa 9 — Notificações e inicialização

**Estado inicial:** PENDENTE  
**Modelo recomendado:** Sonnet 5.5, esforço médio.

## Objetivo

Adicionar notificações discretas e, se solicitado pelo usuário, iniciar o aplicativo com o Windows.

## Passos

1. Confirmar as APIs e permissões atuais de notificações do Windows para o tipo de publicação escolhido.
2. Notificar apenas resultados relevantes: execução finalizada, erro que exige ação e necessidade de reinicialização quando confirmada.
3. Respeitar configurações de privacidade e modo silencioso.
4. Adicionar uma opção explícita para iniciar com o Windows. Desligada por padrão.
5. Usar um mecanismo suportado e fácil de remover. Não escrever no Registro nem criar tarefa escondida sem explicar.
6. Se houver ícone na área de notificação, implementar menu acessível para abrir a janela, verificar estado e sair.
7. Não criar um serviço Windows. Não continuar executando oculto se o usuário não habilitar esse comportamento.
8. Oferecer maneiras claras de desativar inicialização e notificações.

## Critérios de aceite

- Opções ficam visíveis e podem ser revertidas.
- Nenhuma configuração de inicialização é criada sem ação explícita.
- Mensagens não interrompem operações sem necessidade.

## Resultado da execução

Registrar o mecanismo escolhido e testes de ativar/desativar.
