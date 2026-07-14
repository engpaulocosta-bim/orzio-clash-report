# OrzioClashReport

Lê clashes exportados do Navisworks Clash Detective e suporta dois fluxos complementares:
um relatório HTML de uma única run, agrupado por clash test, par de disciplinas e nível, e
uma comparação revision-aware entre duas coordination runs com summary determinístico no
console e HTML lifecycle opcional.

## Arquitetura

Segue Ports and Adapters (arquitetura hexagonal). O core vive em netstandard2.0, sem dependências de terceiros, e não sabe nada sobre a origem dos dados nem sobre o formato de saída. Os adaptadores plugáveis entram nas bordas: parser do XML do Clash Detective como adapter de entrada, renderizador de HTML como adapter de saída. Isso permite trocar a fonte de dados (por exemplo, para a API do Navisworks no futuro) ou o formato de saída sem reescrever o domínio.

## Uso

Requer o .NET SDK fixado em `global.json` (8.0.420).

```bash
dotnet build
dotnet test
```

Gerar o relatório a partir de um export XML do Clash Detective:

```bash
dotnet run --project src/OrzioClashReport.Cli -- <input.xml> -o <output.html>
```

Exemplo com os fixtures em `samples/`:

```bash
dotnet run --project src/OrzioClashReport.Cli -- samples/sample-clash.xml -o report.html
```

A saída no console mostra a contagem bruta vs. agrupada, por exemplo:

```
1458 raw clashes -> 25 groups
Report written to report.html
```

Abra o `report.html` gerado em qualquer navegador — é um arquivo único e autocontido (sem CSS/JS externo).

## Comparar duas coordination runs

O comando `compare` recebe explicitamente os papéis previous/current. Sem output, ele
produz o mesmo resumo determinístico no console. Com `-o`/`--output`, ele escreve esse
mesmo resumo e também gera um HTML revision-aware autocontido:

```bash
dotnet run --project src/OrzioClashReport.Cli -- \
  compare \
  --previous-xml <previous.xml> \
  --previous-manifest <previous.json> \
  --current-xml <current.xml> \
  --current-manifest <current.json> \
  -o comparison.html
```

O pipeline composto pela CLI é:

1. Previous/current são papéis explícitos da linha de comando.
2. A CLI não reordena as runs por timestamp, `RunId`, revisão ou nome de arquivo.
3. Cada XML é parseado separadamente por `NavisworksXmlClashSource`.
4. Cada manifesto é carregado separadamente por `JsonRunManifestSource`.
5. `ExactSourceModelCoordinationRunAssembler` resolve `SourceModel` para `ModelRevision`.
6. `ConservativeClashMatcher` avalia candidate relationships par-a-par.
7. `DeterministicClashRunComparer` seleciona um subconjunto one-to-one determinístico.
8. `ConservativeClashLifecycleClassifier` produz os statuses finais.
9. O console mostra contagens determinísticas de candidates, matches e lifecycle.
10. `-o`/`--output` é opcional: sem output, só há summary; com output, o mesmo summary é
    seguido por um HTML lifecycle revision-aware.
11. Persistência de snapshot de uma única run já existe (ver "Snapshot imutável de uma
    coordination run" abaixo); ledger, índice de runs e histórico ainda não existem, e o
    `compare` ainda não salva nem carrega snapshots automaticamente.
12. Comparar o mesmo fixture nos dois lados é apenas um smoke sintético, não validação sequencial real.

O HTML revision-aware apresenta:

1. metadados das runs previous/current;
2. revisões de modelo declaradas no manifesto;
3. lifecycle summary;
4. matching summary;
5. um card por `ClashLifecycleEntry`;
6. evidências de ocorrência previous/current;
7. confidence do selected match;
8. lifecycle evidence;
9. match evidence.

Limites importantes do fluxo revision-aware:

1. `High` confidence não é confirmação humana.
2. Source clash GUID aparece somente como evidência, não como stable identity.
3. Ainda não existe `Reopened`.
4. Ainda não existe persistent clash id.
5. Já existe persistência de snapshot de uma única run (evidência imutável); ainda não
   existe ledger, índice de runs nem histórico além de duas runs.

### Identidade de um grupo

Um grupo (`ClashGroup`) é identificado pela combinação de três dados, nesta ordem:

1. **Clash test** (`<clashtest name="...">` no export) — clashes de clash tests diferentes
   nunca são misturados no mesmo grupo, mesmo que tenham o mesmo par de disciplinas e nível.
2. **Par de disciplinas**, normalizado de forma independente da ordem A/B.
3. **Nível (`LevelKey`)**: quando os dois elementos do clash estão no mesmo nível, esse nível
   é usado; quando só um lado tem nível, esse é usado; quando os dois lados têm níveis
   diferentes, o resultado é a combinação estável `NívelA × NívelB` (independente da ordem);
   quando nenhum dos dois tem nível, o grupo fica sem nível.

O nome do clash test aparece em `ClashGroup.ClashTestName`, na chave estável do grupo e no
relatório HTML gerado.

### Resolução de disciplina

O agrupamento por par de disciplinas usa `PathHierarchyDisciplineResolver` (em
`OrzioClashReport.Core`) como heurística padrão: tenta o nome do modelo NWD aninhado
(via `pathlink` do export) e cai para a propriedade `Item Source File Name` quando
ausente. Como a nomenclatura de disciplina varia por projeto, essa é uma implementação
plugável de `IDisciplineResolver` — troque por outra se a heurística não bater com as
convenções do seu projeto.

## Samples

Os fixtures em `samples/` (`sample-clash.xml`, `sample-clash2.xml`) contêm dados sintéticos
e anonimizados: nomes de projeto, empresa, caminhos de rede e nomes de ficheiro reais foram
substituídos por valores fictícios, preservando a estrutura do XML e as relações necessárias
para os testes de parsing e agrupamento. Veja [samples/README.md](samples/README.md).

## Estado de validação

- **Compila**: `dotnet build -c Release` passa sem avisos.
- **Roda**: `dotnet test -c Release` está verde e a CLI gera HTML a partir dos fixtures em
  `samples/`, incluindo o smoke sintético do compare com HTML lifecycle.
- **Validado em modelo real**: ainda não. Esta validação só pode ser feita por um humano,
  rodando a ferramenta contra um export real (anonimizado) do Clash Detective e conferindo
  se o relatório agrupado corresponde à realidade do projeto. O fluxo revision-aware ainda
  não foi validado contra exports sequenciais reais.

## CI

`.github/workflows/ci.yml` roda `dotnet build` e `dotnet test` em Release a cada push e pull
request, usando o SDK fixado em `global.json`.

## Run manifest

O manifesto de rodada (`RunManifest`) é uma declaração explícita e auditável de quais
modelos e revisões participaram de uma rodada de coordenação, e de quais clash tests foram
executados nela. Ele não é inferido a partir de nome de arquivo, caminho, XML do
Navisworks, Autodesk Forma ou ACC — nesta etapa do projeto, essa informação é sempre
declarada manualmente.

Exemplo (`samples/run-manifest.sample.json`, schema v2):

```json
{
  "schemaVersion": 2,
  "runId": "coordination-2026-07-10-0900",
  "createdAt": "2026-07-10T09:00:00+01:00",
  "models": [
    {
      "company": "Sigma",
      "discipline": "Structure",
      "modelName": "Sigma_Structure",
      "revision": "R04",
      "sourceFileName": "Sigma_Structure_R04.nwc"
    },
    {
      "company": "Alfa",
      "discipline": "HVAC",
      "modelName": "Alfa_HVAC",
      "revision": "R07",
      "sourceFileName": "Alfa_HVAC_R07.nwc"
    }
  ],
  "executedClashTests": [
    {
      "name": "HVAC vs Structure",
      "modelA": { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure" },
      "modelB": { "company": "Alfa", "discipline": "HVAC", "modelName": "Alfa_HVAC" }
    }
  ]
}
```

Campos obrigatórios de cada item em `models`: `company`, `discipline`, `modelName`,
`revision`, `sourceFileName`. Campos opcionais: `sourceFilePath`, `contentHash`,
`publishedAt`.

A revisão (`revision`) é sempre declarada manualmente — nunca extraída automaticamente do
nome do arquivo ou de qualquer convenção. Dentro da mesma rodada, cada `ModelIdentity`
(company + discipline + modelName, ignorando case) pode ter no máximo uma revisão; um
manifesto que declare duas revisões para a mesma identidade é rejeitado.

### Cobertura explícita de clash tests executados

`executedClashTests` é uma declaração **manual** de quais clash tests rodaram nesta rodada,
distinta e independente das ocorrências de clash observadas:

1. Cada item tem `name` (nome do clash test) e um par ordenado `modelA`/`modelB`, cada um
   com `company`/`discipline`/`modelName` — sem revisão, sem arquivo de origem, sem hash.
   O par é sempre **revision-free**: a mesma declaração cobre qualquer revisão futura desses
   modelos.
2. O par declarado é comparado como **não ordenado** por quem consome a cobertura (A/B
   invertido representa a mesma cobertura), mas o objeto bruto preserva a ordem A/B
   exatamente como declarada.
3. Toda `ClashOccurrence` de um `CoordinationRun` precisa corresponder a um
   `executedClashTests` declarado (mesmo nome, ignorando case, e mesmo par de modelos,
   direto ou invertido); uma ocorrência sem cobertura declarada é rejeitada.
4. Uma declaração pode existir **sem nenhuma occurrence correspondente** — isso é válido e
   é o que permite provar que um clash test rodou e retornou zero clashes, em vez de nunca
   ter rodado. Essa é a funcionalidade principal desta etapa.
5. O lifecycle classifier (abaixo) usa **somente** essa declaração explícita para decidir se
   um clash test foi observado na outra rodada — nunca varre as ocorrências.

O parser (`OrzioClashReport.Input.RunManifestJson`) valida a estrutura e o schema do JSON e
constrói `RunManifest`/`ModelRevision`/`ModelIdentity`/`ExecutedClashTest` do Core. O
comando `compare` da CLI carrega explicitamente um manifesto previous e um current; o
comando legado de HTML continua operando apenas sobre o XML.

### Schema v2 substitui v1

O schema v2 (`schemaVersion: 2`) é a única versão aceita. O schema v1 (`schemaVersion: 1`)
não declarava `executedClashTests` e é **intencionalmente rejeitado**, com uma mensagem
clara indicando que a versão suportada é 2 — não há migração automática nem modo legado.
Migrar silenciosamente um manifesto v1 para uma lista `executedClashTests` vazia
confundiria "nenhum test foi executado" com "não sabemos quais tests foram executados", que
são fatos completamente diferentes.

## Coordination run snapshot

1. `RunManifest` declara quais revisões de modelo participam de uma rodada e quais clash
   tests foram executados nela (ver seções acima).
2. `ClashOccurrence` vincula um `ClashResult` bruto (do XML) às revisões exatas dos modelos
   dos lados A e B dentro de um clash test específico.
3. `CoordinationRun` forma o snapshot imutável: o `RunManifest` mais a lista ordenada de
   `ClashOccurrence`s observadas. Toda revisão usada por uma ocorrência precisa estar
   declarada exatamente no manifesto (mesma `ModelIdentity` com revisão diferente é
   rejeitada), e toda ocorrência precisa corresponder a um `executedClashTests` declarado.

`CoordinationRun` continua sendo o snapshot isolado de uma rodada; matching, comparação e
lifecycle vivem fora dele. A associação entre elementos do XML e as revisões do manifesto
(`ClashObject.SourceModel` → `ModelRevision`) é feita por
`ExactSourceModelCoordinationRunAssembler`, e o comando `compare` da CLI monta
explicitamente uma run previous e uma run current sem inferir ordem temporal.

## Snapshot imutável de uma coordination run

Há dois contratos JSON distintos, com adapters distintos, que não devem ser confundidos:

- **RunManifest JSON** (`OrzioClashReport.Input.RunManifestJson`, `schemaVersion: 2`) é um
  contrato de **entrada explícito, pré-montagem**: declara manualmente modelos, revisões e
  clash tests executados antes de montar a run.
- **CoordinationRun snapshot JSON** (`OrzioClashReport.Persistence.RunSnapshotJson`,
  `schemaVersion: 1`) é um snapshot de **evidência, pós-montagem**: persiste uma
  `CoordinationRun` já montada para que comparação e lifecycle possam ser recalculados no
  futuro.

São schemas diferentes e adapters independentes; o número de `schemaVersion` de um não tem
relação com o do outro.

O adapter público é `JsonCoordinationRunSnapshotSerializer`, com quatro métodos:

- `Serialize(CoordinationRun) -> string` — JSON canônico determinístico.
- `Parse(string) -> CoordinationRun` — reidrata com validação estrita.
- `Save(CoordinationRun, filePath)` — grava um novo arquivo imutável.
- `Load(filePath) -> CoordinationRun` — lê e reidrata.

Características do snapshot (`schemaVersion` 1):

1. A ordem do array `models` é preservada.
2. `executedClashTests` referencia modelos por `modelAIndex`/`modelBIndex`.
3. `occurrences` referencia modelos por `modelAIndex`/`modelBIndex`.
4. A ordem das ocorrências e os slots duplicados são preservados.
5. A reidratação reusa as instâncias exatas de `ModelRevision`/`ModelIdentity` apontadas
   pelos índices.
6. O `ClashStatus` bruto (recebido da fonte) é persistido como string exata do enum.
7. Matching, seleção, confiança, evidências e lifecycle **não** são persistidos — são
   recalculáveis e nunca são congelados na camada de evidência. `ClashStatus.Resolved` bruto
   é evidência da fonte, não um lifecycle status.
8. `ClashObject.Properties` é a única coleção canonicalizada: as entradas são ordenadas por
   chave com `StringComparer.Ordinal` antes de serializar.
9. Nomes de propriedade são camelCase exato e case-sensitive; propriedades JSON desconhecidas
   ou duplicadas são rejeitadas. Timestamps exigem offset explícito ou `Z`.
10. `Save` usa semântica create-new: nunca sobrescreve um arquivo existente (mesmo com bytes
    idênticos), grava UTF-8 sem BOM, e uma falha de serialização não cria arquivo.

Exemplo reduzido:

```json
{
  "schemaVersion": 1,
  "runId": "coordination-run-001",
  "createdAt": "2026-07-14T09:00:00.0000000+01:00",
  "models": [
    {
      "company": "Sigma",
      "discipline": "Structure",
      "modelName": "Main",
      "revision": "R04",
      "sourceFileName": "Sigma_Main_R04.nwc",
      "sourceFilePath": null,
      "contentHash": null,
      "publishedAt": null
    }
  ],
  "executedClashTests": [
    {
      "name": "Structure self clash",
      "modelAIndex": 0,
      "modelBIndex": 0
    }
  ],
  "occurrences": []
}
```

Criação de snapshot pela CLI:

```bash
dotnet run --project src/OrzioClashReport.Cli -- \
  snapshot \
  --xml samples/sample-clash.xml \
  --manifest samples/sample-clash.run-manifest.json \
  -o run-snapshot.json
```

O comando `snapshot` compõe explicitamente o pipeline real:

1. `NavisworksXmlClashSource` lê o XML.
2. `JsonRunManifestSource` carrega o manifesto.
3. `ExactSourceModelCoordinationRunAssembler` monta a `CoordinationRun`.
4. `JsonCoordinationRunSnapshotSerializer.Save` persiste o snapshot canônico imutável.

Regras do contrato da CLI:

1. `-o`/`--output` é obrigatório.
2. A CLI não infere nome de ficheiro nem convenção de armazenamento.
3. A CLI não cria o diretório-pai do output.
4. Um caminho de output já existente é recusado.
5. O success summary só é impresso depois que `Save` conclui com sucesso.

Success summary esperado com o fixture real:

```text
Run snapshot: coordination-sample-clash-xml
Models: 2
Executed clash tests: 2
Occurrences: 5
Snapshot written to run-snapshot.json
```

Este comando cria um único snapshot imutável de run. Ele não compara snapshots, não adiciona
a run a uma coleção ou histórico, não cria ledger, e não persiste matching ou lifecycle:
essas informações continuam recalculáveis.

Limites honestos desta etapa:

- Já existe criação de snapshot pela CLI; ainda não há carregamento/comparação de snapshots pela CLI.
- Ainda não há Clash Ledger.
- Ainda não há travessia de histórico.
- Ainda não há `Reopened`.
- Ainda não há validação sequencial contra exports reais.

## Matching vocabulary

1. Os contratos `ClashMatchConfidence`, `MatchEvidence` e `ClashMatchAssessment` ainda não
   executam nenhum matching — são apenas o vocabulário que uma futura implementação usará
   para registrar e justificar uma avaliação.
2. `ClashMatchConfidence.High` **não** significa "exato" nem "confirmado por um humano"; é
   apenas o nível mais alto de corroboração por evidências.
3. O GUID do clash reportado pela fonte (`MatchEvidenceKind.SourceClashGuid`) é apenas mais
   uma evidência — ainda não foi provado estável entre exports sequenciais reais.
4. Cada `MatchEvidence` tem um veredito (`Supports`, `Contradicts` ou `Unavailable`);
   `ClashMatchAssessment` permite vereditos mistos e não recalcula a confiança a partir
   deles.
5. Não há score numérico, threshold, lifecycle status ou decisão automática nestes
   contratos.
6. O comando `compare` usa esses contratos via matcher/comparer/classifier existentes; a
   CLI apenas apresenta o resultado.

## Pairwise matcher port

1. `IClashMatcher` (`src/OrzioClashReport.Core/Abstractions/IClashMatcher.cs`) avalia um par
   ordenado anterior/atual de `ClashOccurrence` — nada mais.
2. Um retorno não nulo (`ClashMatchAssessment`) é uma avaliação candidata, com confiança e
   evidências auditáveis.
3. `null` significa que o matcher não produziu nenhum candidato para aquele par (evidência
   insuficiente, sinais incompatíveis, ou pré-condição da estratégia não satisfeita).
4. `Low` **não** equivale a `null` — `Low` é uma avaliação candidata real, com pelo menos uma
   evidência; `null` é a ausência de avaliação.
5. O port não define o algoritmo concreto; a implementação de produção atual é
   `ConservativeClashMatcher`.
6. O port não compara rodadas (`CoordinationRun`) completas nem recebe listas de ocorrências.
7. Seleção um-para-um entre candidatos concorrentes, resolução de conflitos e qualquer
   lifecycle status ainda não existem — ficam para um futuro run comparer.
8. O comando `compare` usa o matcher apenas através de `DeterministicClashRunComparer`.

## Conservative pairwise matcher

`ConservativeClashMatcher` (`src/OrzioClashReport.Core/Matching/ConservativeClashMatcher.cs`)
é a primeira implementação concreta de `IClashMatcher`.

1. Ele exige três sinais obrigatórios ao mesmo tempo: mesmo `ClashTestName` (ordinal,
   ignorando case), mesmo par revision-free de `ModelIdentity`, e o par de `ElementId`
   alinhado a esses modelos.
2. Revisões (`ModelRevision.Revision`, `SourceFileName`, `SourceFilePath`, `ContentHash`,
   `PublishedAt`) são completamente ignoradas no matching — o candidato sobrevive a `R03 → R04`.
3. Inversão A/B entre exports é aceita: os dois modelos podem trocar de lado entre a rodada
   anterior e a atual, desde que os elementos acompanhem a mesma troca.
4. `ElementId` e o GUID da fonte são tratados como identificadores opacos e comparados com
   `StringComparison.Ordinal` (case-sensitive) — nunca `OrdinalIgnoreCase`.
5. O GUID da fonte é evidência suplementar: um GUID igual eleva a confiança de `Medium` para
   `High`; um GUID diferente ou ausente **não** cria nem destrói um candidato.
6. Resultado `High` exige os três sinais obrigatórios **e** GUID igual; `Medium` ocorre
   quando os três sinais passam mas o GUID está ausente ou contradiz.
7. Este matcher nunca produz `Low` — ele só aceita quando os três sinais obrigatórios são
   favoráveis; candidatos fracos ficam para uma estratégia futura.
8. A comparação entre rodadas completas e a classificação de lifecycle acontecem em
   componentes separados; este matcher permanece estritamente pairwise.
9. O comando `compare` usa este matcher na composição revision-aware atual.

## Deterministic run comparer

`DeterministicClashRunComparer` (`src/OrzioClashReport.Core/Matching/DeterministicClashRunComparer.cs`)
implementa `IClashRunComparer`, o primeiro orquestrador entre duas `CoordinationRun`.

1. Recebe explicitamente `previousRun` e `currentRun` — nunca infere qual é qual por
   `CreatedAt` ou `RunId`.
2. Avalia todos os pares (previous × current) através de um `IClashMatcher` injetado.
3. Preserva todos os candidatos gerados (`Candidates`), mesmo os não selecionados.
4. Seleciona um subconjunto um-para-um (`SelectedMatches`): nenhum índice anterior ou atual
   repete entre os selecionados.
5. Precedência de seleção: `High > Medium > Low`.
6. Desempate por `PreviousIndex` crescente, depois `CurrentIndex` crescente.
7. Candidatos não selecionados continuam visíveis e auditáveis em `AlternativeCandidates` —
   nunca são tratados como falsos.
8. `UnmatchedPrevious`/`UnmatchedCurrent` **não é lifecycle status** — uma ocorrência sem
   match selecionado ainda pode ter candidatos alternativos.
9. A política é **greedy e não é globalmente ótima**: uma seleção feita cedo pode bloquear
   dois candidatos que uma atribuição ótima teria conseguido parear. Isso é aceitável nesta
   etapa porque a política é determinística, a precedência é explícita, e nenhuma
   classificação de lifecycle é produzida a partir do resultado.
10. O comando `compare` usa este comparer com papéis previous/current explícitos, sem
    inferência temporal.

## Conservative lifecycle classification

`ConservativeClashLifecycleClassifier` (`src/OrzioClashReport.Core/Lifecycle/ConservativeClashLifecycleClassifier.cs`)
implementa `IClashLifecycleClassifier`: classifica cada slot de um `ClashRunMatchResult` já
produzido, sem nunca reexecutar `IClashMatcher` ou `IClashRunComparer`.

1. **`StillOpen`**: um match selecionado com confiança `Medium` ou `High` **e** nenhum
   candidato alternativo compartilhando seu `PreviousIndex` ou `CurrentIndex`.
2. **`Resolved`**: uma ocorrência anterior sem match selecionado, **sem** candidato
   alternativo referenciando seu índice, com ambos os `ModelIdentity` (revision-free) e o
   clash test observados na rodada atual.
3. **`New`**: regra simétrica — uma ocorrência atual sem match selecionado, sem
   alternativa, com modelos e clash test observados na rodada anterior.
4. **`Unverifiable`**: qualquer coisa que não satisfaça as condições acima — confiança
   `Low`, candidato alternativo concorrente, modelo ausente, ou clash test não observado.
5. Cobertura é sempre revision-free: `ModelRevision.Revision`, `SourceFileName`,
   `SourceFilePath`, `ContentHash` e `PublishedAt` nunca participam da verificação.
6. Um clash test só é considerado **observado** numa rodada quando o
   `RunManifest.ExecutedClashTests` **dessa rodada** declara explicitamente esse nome
   (comparação ordinal, ignorando case) para o mesmo par de `ModelIdentity`, direto ou
   invertido — nunca varrendo `CoordinationRun.Occurrences`. Isso é o que permite provar que
   um test rodou e retornou zero clashes: uma rodada pode declarar um `ExecutedClashTest`
   sem nenhuma `ClashOccurrence` correspondente, e essa declaração sozinha já é evidência
   suficiente para `Resolved`/`New` no lado oposto.
7. O `ClashStatus` bruto (vindo do Clash Detective) nunca participa da decisão de
   lifecycle.
8. Não existe `Reopened` — distinguir um clash genuinamente novo de um que reabriu exige
   histórico de mais de duas rodadas, fora do escopo desta etapa.
9. O comando `compare` usa esse classificador e imprime apenas um resumo determinístico no
   console.

## Coordination run assembly

`ExactSourceModelCoordinationRunAssembler` (`src/OrzioClashReport.Core/Assembly/ExactSourceModelCoordinationRunAssembler.cs`)
implementa `ICoordinationRunAssembler`: o primeiro assembler que conecta os dois adapters
já existentes, produzindo um `CoordinationRun` a partir de um `ClashReportDocument` (parser
XML) e um `RunManifest` (adapter JSON).

1. O parser XML produz `ClashReportDocument`; o adapter JSON do manifesto produz
   `RunManifest`. `ExactSourceModelCoordinationRunAssembler` combina os dois — nenhum dos
   dois adapters depende do outro, e o assembler vive inteiramente no Core, sem I/O.
2. Cada lado do clash (`ClashResult.ElementA`/`ElementB`) é resolvido exclusivamente via
   `ClashObject.SourceModel`, comparado contra `ModelRevision.SourceFileName` ou
   `SourceFilePath` de cada modelo declarado no manifesto.
3. A única normalização permitida é `Trim()`; a comparação é
   `StringComparison.OrdinalIgnoreCase`. Nenhuma heurística de nome de arquivo é aplicada:
   sem `Path.GetFileName`, sem remoção de extensão, sem normalização de separador de
   diretório, sem substring/prefix/suffix, sem regex, sem fuzzy matching, sem inferência de
   revisão/disciplina/empresa a partir do token.
4. Zero modelo correspondente no manifesto é falha (`CoordinationRunAssemblyException`).
5. Mais de um `ModelRevision` distinto correspondendo ao mesmo `SourceModel` também é falha
   — ambiguidade nunca é resolvida por "primeiro candidato" ou qualquer outro critério
   automático.
6. A ordem documental (batch-major, clash-minor) e a orientação A/B são sempre preservadas;
   duplicidades no documento viram `ClashOccurrence`s duplicadas, nunca deduplicadas.
7. `CoordinationRun` continua sendo a autoridade final para validar cobertura de
   `ExecutedClashTest` — o assembler não duplica essa regra, apenas deixa que a construção
   final de `CoordinationRun` a aplique.
8. Existe um manifesto companion sintético vinculado ao fixture XML real
   (`samples/sample-clash.run-manifest.json`, para `samples/sample-clash.xml`) — ver a seção
   abaixo.
9. O comando `compare` executa esse pipeline revision-aware para cada lado explicitamente.
10. Ainda não foi validado em modelo real sequencial.

### Companion manifest para `sample-clash.xml`

`samples/sample-clash.run-manifest.json` declara manualmente os modelos e clash tests
necessários para o `NavisworksXmlClashSource` real conseguir montar um `CoordinationRun` a
partir de `samples/sample-clash.xml`, usando `ExactSourceModelCoordinationRunAssembler`. A
inspeção do fixture (via o parser já corrigido) mostrou:

- 1 batch: `"Teste 01"`, com 5 clashes.
- **1 único token distinto de `SourceModel`** em todos os 5 clashes, nos dois lados:
  `"Project_A_HVAC_PD_R00.rvt"` — ou seja, o fixture é um cenário de **self-clash** (o
  mesmo modelo contra si mesmo).

O manifesto declara `ModelRevision.SourceFileName = "Project_A_HVAC_PD_R00.rvt"` (igual ao
token exato produzido pelo parser) e um `ExecutedClashTest` self-clash para `"Teste 01"`. Um
segundo modelo sintético (`Beta_Architecture_R10.nwc`) e um `ExecutedClashTest` de zero
occurrences entre ele e o modelo do HVAC também estão declarados, apenas para ilustrar a
funcionalidade de zero-result introduzida na Etapa 10 — nenhum dos dois é necessário para o
binding do fixture real (ver `samples/README.md`).

## Backlog (fora do MVP)

Esta seção existe para registrar pedidos que não entram no MVP.

- Imagens de clash embutidas no relatório
- Exportação em PDF
- Licenciamento
- UI WPF
- Adaptador da API do Navisworks (.NET API, leitura ao vivo)
- Edição de status do clash dentro da ferramenta
- Integração com CDE (Common Data Environment)
