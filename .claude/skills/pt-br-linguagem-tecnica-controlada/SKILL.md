---
name: pt-br-linguagem-tecnica-controlada
description: Criar, revisar e simplificar documentação técnica em português brasileiro, com vocabulário consistente, frases claras e instruções verificáveis. Usa princípios transferíveis da ASD-STE100 Issue 9 sem tratar a adaptação como norma oficial.
---

# Português Brasileiro — Linguagem Técnica Controlada

## Objetivo

Produza textos técnicos em português brasileiro que sejam claros, precisos e fáceis de seguir. O leitor deve entender a informação ou executar a tarefa sem precisar adivinhar o significado de uma frase.

Use esta skill em documentação de sistemas, manuais, procedimentos, tutoriais, instruções para usuários, textos de interface, mensagens de erro, requisitos de software, critérios de aceite, tickets, README, notas de versão e conteúdo de suporte.

## Relação com a ASD-STE100 Issue 9

Esta skill foi atualizada com base nos princípios de escrita e na organização da **ASD-STE100 Issue 9, de 15 de janeiro de 2025**, fornecida como referência para o projeto.

A ASD-STE100 é um padrão de inglês técnico controlado. Sua Parte 1 tem nove seções de regras; sua Parte 2 contém um dicionário controlado para o inglês. A skill em português adapta princípios transferíveis, como controle de termos, clareza de instruções, estrutura lógica, cuidado com referências, escrita de segurança e consistência editorial.

**Esta skill não é uma tradução oficial da ASD-STE100, não é uma norma publicada chamada PTC e não certifica conformidade com o padrão inglês.** Neste projeto, *Português Técnico Controlado (PTC)* é apenas o nome de uma convenção editorial proposta.

Não transfira automaticamente para o português regras gramaticais específicas do inglês. Isso inclui o limite de três palavras para grupos nominais, as formas verbais permitidas, a restrição ao ponto e vírgula, as regras do sufixo *-ing*, a grafia americana e os limites formais de contagem de palavras em inglês. Use os princípios de clareza e consistência, mas aplique a gramática e as convenções corretas do português brasileiro.

## 1. Entenda o leitor e a tarefa

Antes de escrever, identifique:

- quem vai ler o texto;
- qual informação a pessoa precisa entender;
- qual ação precisa executar, se houver;
- quais fatos, condições, limites e resultados precisam ser preservados;
- quais nomes técnicos e termos da interface são obrigatórios.

Use palavras e explicações adequadas ao público. Evite jargão para usuários finais. Para desenvolvedores e profissionais técnicos, preserve os termos de domínio necessários e explique-os quando isso ajudar.

## 2. Escreva de forma direta

- Coloque a informação principal ou a ação no início.
- Prefira palavras comuns, concretas e precisas.
- Use verbos claros no lugar de expressões burocráticas e substantivos abstratos.
- Prefira a voz ativa quando ela deixar claro quem executa a ação.
- Evite frases com muitas ideias, orações encaixadas ou explicações entre parênteses que interrompam a leitura.
- Não transforme o texto em uma sequência telegráfica. Mantenha as palavras necessárias para preservar sentido, relação lógica e fluidez natural em português.
- Use conectivos quando eles explicarem relações de causa, condição, contraste, sequência ou consequência.
- Remova repetições que não ajudam; mantenha a repetição de um termo quando ela evitar ambiguidade.

Exemplo:

**Evite:** “Para que seja possível efetuar a alteração desejada, deverá ser realizada a seleção do registro correspondente e, posteriormente, deverá ser acionado o recurso de gravação.”

**Prefira:** “Abra o cadastro. Selecione o registro que deseja alterar. Edite os campos necessários e clique em **Salvar**.”

## 3. Controle a terminologia

Use a mesma palavra para o mesmo conceito em todos os textos relacionados.

- Mantenha um glossário ou uma lista de termos preferidos quando o projeto tiver vários documentos.
- Registre o termo preferido, o significado no contexto do sistema, as alternativas a evitar e a grafia oficial.
- Não alterne entre sinônimos apenas para evitar repetição.
- Preserve nomes de telas, menus, campos, botões, rotas, APIs, endpoints, tabelas, classes, variáveis, arquivos e comandos exatamente como existem.
- Preserve o texto literal de mensagens, rótulos e controles quando o leitor precisar localizar o elemento na interface.
- Explique siglas e termos pouco conhecidos na primeira ocorrência, quando o público precisar dessa explicação.
- Não traduza nem renomeie um identificador técnico sem uma solicitação explícita.
- Não substitua um termo preciso por outro mais simples se isso alterar o significado.

Quando o termo oficial não estiver confirmado, não invente um nome. Marque-o como pendente ou solicite esclarecimento.

## 4. Elimine ambiguidades

Especifique quem faz a ação, o que deve ser feito, em qual condição e qual resultado é esperado, quando esses detalhes forem relevantes.

- Reescreva pronomes como “ele”, “ela”, “isso”, “esse” ou “aquele” quando houver mais de um referente possível.
- Coloque cada condição próxima da regra ou ação a que ela se aplica.
- Diferencie fato confirmado, hipótese e informação desconhecida.
- Distinga obrigação, permissão e proibição. Use “deve”, “pode” e “não pode” com o sentido correto.
- Evite expressões vagas como “normalmente”, “adequadamente” ou “conforme necessário” quando a regra exigir um comportamento verificável.
- Não invente causas, comportamentos do sistema, prazos, resultados ou restrições.

## 5. Escreva procedimentos e instruções para usuários

Organize cada procedimento para que o leitor consiga executar a tarefa na ordem correta.

1. Informe o pré-requisito, se houver.
2. Identifique a tela, menu ou recurso pelo nome real.
3. Apresente as ações na ordem em que devem ocorrer.
4. Use uma ação principal por etapa. Combine ações somente quando fizerem parte da mesma operação e forem claras em conjunto.
5. Use verbos de ação precisos, como “Abra”, “Selecione”, “Informe”, “Marque”, “Clique”, “Salve” ou “Confirme”, de acordo com o comportamento real.
6. Informe o resultado esperado quando isso ajudar a pessoa a confirmar que terminou.
7. Inclua instruções para erros somente quando o procedimento de recuperação for conhecido.
8. Posicione avisos perto da etapa relacionada ao risco.

Use passos numerados quando a ordem for obrigatória. Use listas simples para opções ou itens sem uma sequência definida. Tente manter as frases curtas, mas não aplique ao português os limites formais de contagem de palavras definidos para o inglês no STE.

## 6. Escreva descrições e explicações

- Apresente informações do geral para o específico ou na sequência necessária para entendê-las.
- Use títulos que expressem o conteúdo da seção.
- Agrupe informações relacionadas no mesmo parágrafo.
- Mantenha um assunto principal por parágrafo.
- Use listas ou tabelas quando elas tornarem relações, opções ou condições mais fáceis de consultar.
- Explique o funcionamento em sequência lógica: componente, função, condição e resultado, conforme o contexto.
- Separe descrição e procedimento quando o leitor precisar diferenciar como algo funciona de como executar uma tarefa.
- Evite parágrafos extensos. Divida-os quando houver mudança de assunto ou quando a leitura ficar difícil.

Não use um número fixo de palavras por frase ou um limite de frases por parágrafo como regra oficial do português. Ajuste a extensão ao conteúdo, ao público e à complexidade.

## 7. Escreva requisitos e instruções para desenvolvimento

Quando transformar uma solicitação em instrução técnica, separe as informações pertinentes:

- **Contexto:** onde ocorre o comportamento ou problema.
- **Comportamento atual:** o que está confirmado que acontece.
- **Comportamento esperado:** o que deve acontecer após a alteração.
- **Condições e exceções:** quando a regra se aplica e quando não se aplica.
- **Escopo:** telas, rotas, APIs, componentes e módulos envolvidos, se conhecidos.
- **Critérios de aceite:** como verificar que o resultado está correto.
- **Restrições:** comportamentos existentes que devem permanecer inalterados.

Escreva critérios que possam ser verificados por uma pessoa ou por um teste. Peça a inspeção do fluxo existente antes de atribuir uma causa a um bug. Não prescreva uma implementação se o requisito define apenas o resultado esperado. Não invente nomes de arquivos, campos, métodos, rotas ou endpoints.

## 8. Escreva mensagens de erro e avisos

Quando os fatos estiverem disponíveis, uma mensagem deve explicar:

1. o que aconteceu;
2. a causa, somente se estiver confirmada;
3. o que a pessoa pode fazer em seguida.

Prefira uma mensagem específica a expressões vagas como “Erro inesperado”, desde que a informação seja verdadeira. Não atribua uma causa provável como se fosse um fato. Não prometa que uma operação foi concluída quando o sistema não confirmou o resultado.

## 9. Trate segurança e risco com cuidado

- Preserve o nível de risco definido pela documentação técnica, pela regulamentação e pelo padrão aplicável.
- Informe a ação preventiva ou a condição necessária com clareza.
- Explique a consequência conhecida quando isso ajudar o leitor a entender o risco.
- Não invente perigos, níveis de severidade ou consequências.
- Não substitua “Perigo”, “Advertência”, “Atenção”, “Cuidado” ou outro termo estabelecido sem consultar a convenção aplicável ao produto ou setor.
- Se o risco ou o nível correto não estiver claro, sinalize a pendência para análise especializada.

A estrutura de segurança da ASD-STE100 pode orientar a revisão — identificação do nível de risco, comando ou condição e explicação da consequência —, mas os rótulos devem seguir as regras aplicáveis ao contexto real.

## 10. Use formatação funcional

- Organize o documento com títulos descritivos e uma hierarquia coerente.
- Use passos numerados para sequências obrigatórias e marcadores para itens sem ordem específica.
- Use tabelas para comparar dados com as mesmas categorias.
- Destaque botões, campos e comandos de forma consistente.
- Preserve blocos de código, SQL, JSON, comandos e identificadores que precisem ser copiados exatamente.
- Use negrito, itálico e avisos com moderação e somente quando ajudarem a encontrar a informação.
- Siga as regras de formatação do projeto ou da organização quando existirem.

A ASD-STE100 controla a expressão linguística, não determina toda a formatação visual do documento. Aplique os padrões visuais em separado.

## 11. Fluxo de trabalho

1. **Entenda:** identifique o público, o objetivo, o formato e a ação ou informação principal.
2. **Separe os fatos:** registre nomes, valores, regras, condições, limites, dependências, exceções e avisos.
3. **Confirme a terminologia:** consulte a interface, a documentação e o glossário disponíveis.
4. **Estruture:** escolha entre procedimento, descrição, requisito, aviso ou mensagem de interface.
5. **Escreva:** use frases diretas, português natural e termos consistentes.
6. **Revise a clareza:** remova burocratês, redundâncias e ambiguidades.
7. **Revise a precisão:** compare o resultado com a fonte e confirme que nenhum fato foi inventado ou alterado.
8. **Entregue:** forneça o conteúdo no formato solicitado. Informe apenas as dúvidas que possam afetar a correção ou a execução.

## Lista de verificação

- O público, o objetivo e a ação principal estão claros?
- A informação principal aparece cedo no texto?
- A frase tem uma ideia principal e usa palavras adequadas ao público?
- O leitor sabe quem deve agir, o que fazer e onde fazer?
- As condições, exceções, consequências e resultados estão explícitos?
- Os termos e nomes da interface permanecem consistentes?
- As obrigações, permissões e proibições estão diferenciadas?
- As instruções estão na ordem correta e podem ser executadas sem adivinhação?
- Os avisos preservam o risco e as ações definidas pela fonte oficial?
- O texto mantém todos os fatos, limites e requisitos?
- Alguma causa, comportamento ou promessa foi inventada?
- As regras específicas do inglês foram indevidamente impostas ao português?

## Comportamento esperado

- Ao criar um texto, entregue diretamente o conteúdo solicitado.
- Ao revisar, corrija clareza, consistência, gramática e estrutura sem alterar o significado.
- Ao simplificar, preserve os detalhes técnicos, as condições, as restrições e as exceções relevantes.
- Ao escrever requisitos, descreva comportamentos verificáveis e diferencie fatos de hipóteses.
- Se faltar uma informação importante, faça uma pergunta objetiva ou marque o ponto como pendente.
- Não inclua introduções longas, justificativas excessivas ou várias versões, a menos que sejam solicitadas.

## Referência

- ASD-STE100, Issue 9, 15 January 2025: https://www.asd-ste100.org/
- Acesso ao padrão e ao dicionário oficiais: https://www.asd-ste100.org/STE_downloads.html
