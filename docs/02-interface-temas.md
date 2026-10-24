# Etapa 2 — Interface e temas

**Estado inicial:** PENDENTE  
**Modelo recomendado:** Sonnet 5.5, esforço médio; Opus 5.5/alto para decisões visuais complexas.

## Objetivo

Criar uma interface com aparência moderna inspirada no Windows 11, com navegação clara, tema e personalização de cor.

## Passos

1. Ler a skill `windows-11-wpf-design`.
2. Inspecionar a implementação atual antes de alterar XAML ou recursos.
3. Pesquisar a documentação oficial atual do WPF para tema Fluent e APIs relacionadas. Confirmar se os recursos são estáveis na versão .NET alvo. Não depender de APIs experimentais sem registrar a limitação.
4. Avaliar se os estilos nativos são suficientes. Se uma biblioteca visual for proposta, verificar manutenção, licença, compatibilidade com .NET 10 e impacto no tamanho da publicação. Não adicionar biblioteca só por estética.
5. Criar um layout principal com navegação discreta: `Atualizações`, `Histórico` e `Configurações`. As páginas futuras podem ter conteúdo provisório, mas devem ser claramente identificadas como não implementadas.
6. Definir tokens/recursos centralizados para plano de fundo, superfície, texto, texto secundário, borda, cor de destaque, hover, seleção, sucesso, aviso e erro.
7. Implementar três modos de aparência: `Sistema`, `Claro` e `Escuro`.
8. Implementar cores predefinidas de destaque e cor personalizada por seletor ou entrada hexadecimal validada. Incluir ação `Restaurar padrão`.
9. Aplicar a cor selecionada a botões principais, foco, seleção, indicador de progresso e controles relevantes. Não colorir todos os elementos indiscriminadamente.
10. Persistir aparência e cor em configuração por usuário, sem exigir administrador.
11. Aplicar mudanças imediatamente sem reiniciar quando tecnicamente possível.
12. Conferir contraste, foco visível, teclado, escalonamento DPI e tamanho de janela redimensionável.
13. Usar efeitos translúcidos com moderação. Priorizar leitura e desempenho; não deixar texto ilegível em fundos com transparência.

## Critérios de aceite

- Tema Sistema/Claro/Escuro funciona.
- A cor de destaque pode ser escolhida, persistida e restaurada.
- Cor hexadecimal inválida é rejeitada sem quebrar o tema.
- O tema não depende de imagens remotas.
- Contraste, foco e navegação por teclado são utilizáveis.
- Preferências continuam após fechar e abrir o aplicativo.

## Verificações

Build; teste manual com cada tema; testar cor clara, escura e personalizada; redimensionar janela; navegar com Tab/Shift+Tab. Se o ambiente não for Windows, registrar a limitação visual.

## Resultado da execução

Preencher com os arquivos, verificações reais e decisões de design. Atualizar `docs/00-plano-geral.md`.
