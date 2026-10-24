# Etapa 8 — Microsoft Store

**Estado inicial:** PENDENTE  
**Modelo recomendado:** Opus 5.5, esforço alto para verificar cobertura e limites; Sonnet 5.5/medio para implementação.

## Objetivo

Integrar a fonte `msstore` do WinGet quando suportada e informar os casos em que a Store deve ser aberta diretamente.

## Passos

1. Ler a documentação oficial do WinGet sobre fontes e a documentação atual da Microsoft Store.
2. Verificar se a fonte `msstore` está configurada, sem alterá-la automaticamente.
3. Usar a origem do pacote como parte da identidade do update. Não confundir um identificador WinGet com um identificador da Store.
4. Confirmar quais comandos e opções são aceitos pela versão instalada antes de executá-los.
5. Não repetir cegamente o comando geral e depois o comando `msstore` se isso causar duplicidade. Evitar duplicar pacotes por ID/origem.
6. Distinguir: pacote encontrado e atualizável pelo WinGet; pacote que requer Store; fonte indisponível; pacote não detectável.
7. Para os casos não gerenciáveis, oferecer botão para abrir a Microsoft Store ou sua página de atualizações, usando uma URI oficial e validada.
8. Não afirmar que a lista contém todos os aplicativos instalados pela Store.
9. Testar com fonte ausente, fonte indisponível, resultado vazio e pacote sem atualização.

## Critérios de aceite

- A origem é visível na lista.
- Pacotes duplicados são tratados com ID + origem.
- Erros da Store não bloqueiam a atualização de pacotes WinGet independentes.
- A aplicação comunica limitações sem declarar cobertura total.

## Resultado da execução

Registrar a versão do WinGet, comportamento da fonte e limitações observadas.
