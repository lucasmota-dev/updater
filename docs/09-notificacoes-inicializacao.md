# Etapa 9 — Notificações e inicialização

**Estado:** CONCLUÍDA
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

### Decisões

- As notificações são banners internos não bloqueantes. Não exigem permissões, não interrompem processos e podem ser fechadas pelo usuário.
- A inicialização é desligada por padrão e usa a tarefa visível `\\Atualizador.Windows.Startup.v1` do Agendador de Tarefas.
- A opção é persistida em `%LOCALAPPDATA%\\Atualizador.Windows\\startup.json` e pode ser revertida pela própria tela de configurações.
- Não foi usado Registro, serviço Windows ou tarefa oculta.

### Verificações

- `dotnet build Atualizador.Windows.slnx --configuration Release --no-restore`: 0 avisos e 0 erros.
- `dotnet test tests/Atualizador.Windows.Tests/Atualizador.Windows.Tests.csproj --configuration Release --no-build`: 21 testes aprovados, 0 falhas.
- O banner interno é publicado para descoberta, erro e conclusão de atualização.
- O comando de inicialização é fixo e usa a conta atual; a criação real da tarefa de inicialização permanece limitada às mesmas restrições documentadas do Agendador da etapa 7.

### Limitações

- Notificações nativas do sistema não foram usadas porque uma publicação WPF não empacotada exige identidade/ativação adicional; o banner interno mantém o comportamento sem permissões extras.
