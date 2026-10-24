---
name: wpf-development
description: Build maintainable WPF applications in C# with XAML, lightweight MVVM, asynchronous services, binding, commands, and testable boundaries.
---

# Desenvolvimento WPF

## Padrões

- C#, WPF e XAML. Confirmar o SDK e o alvo .NET antes de escolher recursos.
- Preferir MVVM leve: View, ViewModel, model e serviço quando a responsabilidade justificar.
- Manter code-behind para comportamento visual estritamente relacionado à janela; não colocar nele regras de negócio ou execução do WinGet.
- Usar propriedades observáveis e comandos com contratos explícitos. Não criar base class, framework MVVM ou camada genérica sem benefício real.
- Manter o trabalho assíncrono fora da thread da interface. Aplicar cancelamento quando fizer sentido.
- Centralizar recursos visuais em dicionários de recursos. Evitar valores de cor duplicados.
- Implementar estados de erro/carregamento/vazio explicitamente.
- Usar `IDisposable`/`IAsyncDisposable` de forma correta quando houver recursos que exigem liberação.
- Não adicionar bibliotecas sem verificar compatibilidade, manutenção, licença e impacto na publicação.
- Preferir tipos explícitos e nullable reference types.

## Testabilidade

- Separar execução de processos e persistência de configurações da lógica de apresentação.
- Testes não devem depender de instalação real de aplicativos, acesso à Microsoft Store ou agendamento real.
- Criar fronteiras simuláveis apenas onde protegem um efeito colateral real; evitar interfaces para cada classe sem motivo.

## Qualidade

- Funções pequenas e com uma responsabilidade clara.
- Evitar arquivos acima de 1.000 linhas sem justificativa forte.
- Não criar classes ou interfaces que apenas encaminham chamadas sem simplificar o desenho.
- Executar build e testes aplicáveis e registrar limitações do ambiente.
