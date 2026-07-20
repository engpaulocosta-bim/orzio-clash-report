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
    coordination run" abaixo), snapshots persistidos podem ser comparados explicitamente
    pela CLI via `compare-snapshots`, e agora também existe um run index JSON ordenado e
    explícito criado pela CLI via `index-snapshots`, além do consumo explícito desse
    índice pela CLI via `compare-index`; a ordem do índice continua sendo a única
    autoridade de sequência, não há discovery automático nem inferência cronológica, e
    não existe ledger nem lifecycle multi-run persistido.
12. Comparar o mesmo fixture nos dois lados é apenas um smoke sintético, não validação sequencial real.

Comparar dois snapshots persistidos, sem reprocessar XML nem reler manifestos:

```bash
dotnet run --project src/OrzioClashReport.Cli -- \
  compare-snapshots \
  --previous-snapshot <previous.json> \
  --current-snapshot <current.json> \
  -o comparison.html
```

Este fluxo carrega dois `CoordinationRun` snapshots persistidos, preserva os papéis
explícitos previous/current exatamente como vieram da linha de comando, recalcula matching
e lifecycle a partir da evidência imutável, e opcionalmente escreve o mesmo HTML
revision-aware do comando `compare`. Ele não cria run collection, run index, history
traversal, ledger, `Reopened` nem persistent clash ID.

Criar um run index ordenado e explícito a partir de snapshots persistidos:

```bash
dotnet run --project src/OrzioClashReport.Cli -- \
  index-snapshots \
  --snapshot <run-001.json> \
  --snapshot <run-002.json> \
  -o run-index.json
```

Este fluxo carrega cada snapshot explicitamente informado apenas para validar existência e
contrato, preserva exatamente a ordem dos `--snapshot` da CLI, converte cada path para uma
referência relativa canónica com `/`, e persiste um run index JSON que guarda somente
`schemaVersion` + `snapshotPaths`. O índice não persiste matching, lifecycle, metadata de
run nem qualquer forma de identidade estável de clash.

Consumir um run index explícito e comparar apenas transições adjacentes:

```bash
dotnet run --project src/OrzioClashReport.Cli -- \
  compare-index \
  --index run-index.json
```

Regras do contrato:

1. `--index` é obrigatório e único.
2. `JsonRunIndexSerializer.Load` é a única autoridade para carregar o índice.
3. A ordem de `snapshotPaths` é a única sequência autoritativa.
4. Cada referência é resolvida por `RunIndexSnapshotPathResolver.ResolveReference`.
5. Cada snapshot resolvido é carregado por `JsonCoordinationRunSnapshotSerializer.Load`.
6. Todos os snapshots são carregados antes de qualquer output.
7. A travessia adjacente é feita pelo Core, por
   `DeterministicAdjacentClashRunSequenceComparer` (ver "Comparador de sequência de runs
   adjacentes" abaixo), que calcula todas as comparações adjacentes antes de qualquer output.
8. Os pares são exatamente `[i] -> [i + 1]`, preservando duplicados e a ordem declarada.
9. Matching e lifecycle são recalculados independentemente para cada transição adjacente.
10. O comando reutiliza o mesmo summary pairwise determinístico de 11 linhas já usado por
    `compare` e `compare-snapshots`.
11. `compare-index` é console-only nesta etapa: não aceita `-o`/`--output` e não gera HTML.
12. Não há discovery automático, inferência cronológica, latest/previous lookup,
    comparação non-adjacent, all-vs-all, Clash Ledger, `Reopened` nem persistent clash ID.

### Comparador de sequência de runs adjacentes (Core)

`IClashRunSequenceComparer` (`src/OrzioClashReport.Core/Abstractions/IClashRunSequenceComparer.cs`)
formaliza no Core a travessia adjacente que antes vivia diretamente no loop do `compare-index`
em `Program.cs`. Ele recebe uma `IReadOnlyList<CoordinationRun>` já explicitamente ordenada
pelo chamador — o Core não conhece run-index JSON nem qualquer outro formato de persistência
— e compara somente pares `[i] -> [i + 1]`, nunca pares non-adjacent ou invertidos.

`DeterministicAdjacentClashRunSequenceComparer` é a única implementação atual. O construtor
recebe um `IClashRunComparer` e um `IClashLifecycleClassifier`; para cada par adjacente ele
chama o run comparer injetado e depois o lifecycle classifier injetado, sem propagar match
selecionado, confiança ou evidência de uma transição para a próxima. Exige pelo menos duas
runs, rejeita qualquer entrada nula e rejeita uma sequência nula. Referências duplicadas de
run (por exemplo `A, A, B`) são preservadas, nunca deduplicadas. A travessia é síncrona,
sequencial e fail-fast: uma exceção de qualquer dependência injetada em qualquer par se
propaga imediatamente, e nenhum `ClashRunSequenceComparisonResult` parcial é retornado.

`ClashRunSequenceComparisonResult` é o resultado imutável: as `Runs` ordenadas mais um
`ClashLifecycleResult` por transição adjacente em `Comparisons`, na mesma ordem. Ele valida
somente continuidade estrutural — cada `Comparisons[i]` precisa referenciar `Runs[i]` e
`Runs[i + 1]` por **exact object reference** (não `RunId`, não `CreatedAt`, não value
equality) como seus lados previous/current — e nunca recalcula matching ou lifecycle.
Representa apenas uma coleção ordenada de resultados lifecycle pairwise adjacentes
recalculados independentemente: não cria history, lifecycle multi-run, persistent clash
identity, Clash Ledger nem `Reopened`. `compare-index` é o único consumidor atual; `compare`
e `compare-snapshots` continuam pairwise, usando o helper `CreateDerivedComparison` já
existente em `Program.cs`, não este sequence comparer.

### Projeção de continuidade de selected matches (somente Core, ainda não exposta em nenhuma CLI)

`IClashRunSequenceContinuityProjector` (`src/OrzioClashReport.Core/Abstractions/IClashRunSequenceContinuityProjector.cs`)
projeta um `ClashRunSequenceComparisonResult` já derivado sobre o conjunto de
`SelectedMatchContinuityLink`s existentes em suas fronteiras de comparações consecutivas:
na fronteira `i` (entre `Comparisons[i]` e `Comparisons[i + 1]`, compartilhando a run em
`Runs[i + 1]`), existe um link sempre que o `CurrentIndex` de um selected match entra num
slot de ocorrência e o `PreviousIndex` de um selected match sai exatamente do mesmo slot. O
projector não conhece run-index JSON, não carrega snapshots, e não chama `IClashMatcher`,
`IClashRunComparer`, `IClashLifecycleClassifier` nem `IClashRunSequenceComparer` — matching,
comparação de runs, classificação de lifecycle e sequence comparison já aconteceram antes
dele rodar.

`DeterministicSelectedMatchContinuityProjector` é a única implementação atual, com
construtor público sem dependências. Considera somente
`ClashRunMatchResult.SelectedMatches` — `Candidates`, `AlternativeCandidates`,
`UnmatchedPrevious` e `UnmatchedCurrent` nunca criam link, e o `ClashLifecycleStatus` de um
selected match também nunca filtra a projeção (um selected match classificado
`Unverifiable` ainda pode gerar link). Somente fronteiras consecutivas são consideradas —
não há comparação non-adjacent nem `[0]` direto para `[2]`, e duplicados (mesma referência
de run ou mesmo `RunId`) nunca são deduplicados.

`SelectedMatchContinuityLink` observa somente que um selected match entra num slot exato de
uma run compartilhada e outro selected match sai do mesmo slot exato através da comparação
imediatamente seguinte. Guarda `IncomingComparisonIndex` e `SharedOccurrenceIndex`;
`OutgoingComparisonIndex` e `SharedRunIndex` são derivados (`IncomingComparisonIndex + 1`).
Valida continuidade exata de slot e de referência de objeto — equivalência value-shaped num
slot diferente nunca satisfaz o link. Não carrega identificador, fingerprint, status nem
confidence agregada.

`ClashRunSequenceContinuityResult` é o resultado imutável: a referência exata de
`SequenceComparison` mais o conjunto completo e canonicamente ordenado (`IncomingComparisonIndex`
ascendente, depois `SharedOccurrenceIndex` ascendente) de `Links`. Ele revalida
independentemente cada link — membership exata nos selected matches (nunca um
alternative nem um objeto equivalente-mas-distinto), referência da run compartilhada,
continuidade do slot compartilhado, e completude: recalcula, a partir apenas de
`SequenceComparison`, o conjunto esperado de pares (boundary, slot) e exige que `Links`
corresponda exatamente, posição a posição — o que rejeita link faltante, link extra, link
duplicado e qualquer ordem não canônica, tudo numa única verificação estrutural. Isso não é
rematching.

Esta é a menor observação longitudinal possível e para bem antes de identidade de clash: um
link nunca afirma que o clash subjacente é o mesmo clash. Já existe montagem determinística
de paths máximos de continuidade (ver abaixo); persistent tracking, ledger, identidade,
agregação de lifecycle e `Reopened` continuam não existindo. Links são derivados e
recalculáveis; nunca são persistidos. Nenhum comando CLI e nenhum renderer HTML consome
essa projeção ainda — o stdout do `compare-index` permanece inalterado, e `Program.cs` não
é tocado por esta projeção. Validação sequencial contra exports reais do Navisworks
continua não verificada.

### Montagem determinística de paths máximos de continuidade (somente Core, ainda não exposta em nenhuma CLI)

`IClashRunSequenceContinuityPathAssembler`
(`src/OrzioClashReport.Core/Abstractions/IClashRunSequenceContinuityPathAssembler.cs`)
monta um `ClashRunSequenceContinuityResult` já derivado no conjunto completo de paths de
continuidade máximos e disjuntos implicados pelos seus links: dois links pertencem ao mesmo
path somente quando o `OutgoingSelectedMatch` do primeiro é exatamente a mesma referência de
objeto que o `IncomingSelectedMatch` do segundo, na fronteira de comparação imediatamente
seguinte (`next.IncomingComparisonIndex == current.OutgoingComparisonIndex`). O assembler não
conhece JSON, snapshot nem filesystem, e não chama `IClashMatcher`, `IClashRunComparer`,
`IClashLifecycleClassifier`, `IClashRunSequenceComparer` nem
`IClashRunSequenceContinuityProjector` — matching, comparação de runs, classificação de
lifecycle, sequence comparison e continuity projection já aconteceram antes dele rodar.

`DeterministicSelectedMatchContinuityPathAssembler`
(`src/OrzioClashReport.Core/Continuity/DeterministicSelectedMatchContinuityPathAssembler.cs`)
é a única implementação atual, com construtor público sem dependências. Para cada link de
`ContinuityResult.Links` em ordem canônica, verifica se existe um predecessor exato (um link
cujo `OutgoingComparisonIndex` e `OutgoingSelectedMatch` correspondem exatamente ao
`IncomingComparisonIndex` e `IncomingSelectedMatch` do link atual); um link sem predecessor
inicia um novo path, que então segue sua cadeia de sucessores exatos até não haver mais
nenhum. A conectividade nunca usa `RunId`, `CreatedAt`, índices de candidate isolados,
referência de occurrence isolada, value equality de candidate ou assessment, GUID da fonte,
confidence, evidence, `ToString`, hash ou fingerprint — somente identidade exata de
referência de objeto do selected match. Zero links produzem zero paths, e nenhum path
vazio é criado. Como as invariantes atuais garantem no máximo um predecessor exato e um
sucessor exato por link, o assembler detecta defensivamente mais de um de qualquer um deles
(impossível pela construção normal, mas uma defesa contra corrupção ou regressão futura) e
lança `InvalidOperationException` em vez de escolher o primeiro silenciosamente.

`SelectedMatchContinuityPath`
(`src/OrzioClashReport.Core/Model/SelectedMatchContinuityPath.cs`) é uma sequência máxima
imutável de `SelectedMatchContinuityLink`s conectados somente por essa regra de referência
exata. Seu construtor interno rejeita links nulos/vazios, um slot de link nulo, boundary
repetida ou invertida, gap de boundary, e uma referência de candidate distinta mas
value-equivalente em qualquer junção. `SelectedMatches` é derivado, nunca fornecido:
`Links[0].IncomingSelectedMatch` seguido do `OutgoingSelectedMatch` de cada link, então
`SelectedMatches.Count == Links.Count + 1`. `StartComparisonIndex`/`EndComparisonIndex`/
`StartRunIndex`/`EndRunIndex` são derivados do primeiro e último link, nunca armazenados
redundantemente. O path não carrega id, status, fingerprint nem confidence agregada — afirma
somente que esses links de continuidade exatos formam uma sequência máxima conectada por
referência exata, nunca que o clash subjacente é uma entidade persistente única, nem que o
path tem identidade estável ou sobrevive à recomputação por id.

`ClashRunSequenceContinuityPathsResult`
(`src/OrzioClashReport.Core/Model/ClashRunSequenceContinuityPathsResult.cs`) é o resultado
imutável: a referência exata de `ContinuityResult` mais o conjunto completo e canonicamente
ordenado de `Paths`. A ordem canônica é a posição do primeiro link de cada path em
`ContinuityResult.Links` — nunca comprimento do path, `RunId`, `CreatedAt`, confidence, GUID
da fonte ou detalhes de occurrence. Ele revalida independentemente a partição máxima completa
recalculando, somente a partir de `ContinuityResult.Links`, a mesma conectividade de
predecessor/sucessor que o assembler usa, e exige que `Paths` corresponda exatamente: mesma
contagem de paths, mesma ordem canônica, mesma contagem de links por path, e as mesmas
referências exatas de link em cada posição. Essa única comparação estrutural é o que rejeita
path faltante, path extra, path duplicado, ordem de path errada, link estrangeiro ou
equivalente-mas-distinto, link faltante ou extra dentro de um path, cobertura duplicada de
link, split de um path máximo, merge de paths desconectados, e path não máximo, tudo de uma
vez — nunca rematching nem reinvocação do assembler.

Um continuity path é uma sequência derivada, máxima e totalmente recalculável de continuity
links exatos de selected match; não é persistent clash identity, stable clash identity,
Clash Ledger nem persistent track, não tem history nem lifecycle multi-run, e não implica
`Reopened`. Um selected match sem nenhum continuity link nunca aparece em path algum, e
nenhum path vazio é criado. Isso é somente Core: nenhum comando CLI e nenhum renderer HTML
consome ainda, o stdout do `compare-index` permanece inalterado, e `Program.cs` não é
tocado. Validação sequencial contra exports reais do Navisworks continua não verificada.

### Orquestrador de analise longitudinal de sequencia (somente Core, ainda nao exposto em nenhuma CLI)

`IClashRunSequenceAnalyzer`
(`src/OrzioClashReport.Core/Abstractions/IClashRunSequenceAnalyzer.cs`) e a fronteira unica
do Core que compoe, na ordem declarada pelo chamador:
`IClashRunSequenceComparer` -> `IClashRunSequenceContinuityProjector` ->
`IClashRunSequenceContinuityPathAssembler`. A ordem recebida continua sendo a unica
autoridade; o analyzer nao ordena, nao deduplica, nao infere cronologia, nao compara runs
non-adjacent, nao persiste estado derivado e nao cria history, Clash Ledger, lifecycle
multi-run, `Reopened` ou identidade estavel/persistente de clash.

`DeterministicClashRunSequenceAnalyzer`
(`src/OrzioClashReport.Core/Analysis/DeterministicClashRunSequenceAnalyzer.cs`) recebe as
tres portas no construtor, rejeita dependencias nulas, rejeita `runs` nulo antes de chamar
qualquer dependencia, chama cada estagio exatamente uma vez na ordem definida, passa a
referencia exata do resultado de um estagio para o proximo, e propaga excecoes sem embrulhar
nem devolver resultado parcial. Ele e sincrono, deterministico e nao faz I/O, clock, rede,
aleatoriedade, DI container, matching, lifecycle classification, continuity projection ou
path assembly por conta propria.

`ClashRunSequenceAnalysisResult`
(`src/OrzioClashReport.Core/Model/ClashRunSequenceAnalysisResult.cs`) e o aggregate imutavel:
preserva as referencias exatas de `SequenceComparison`, `ContinuityResult` e
`ContinuityPathsResult` de uma cadeia derivada coerente. O construtor interno rejeita nulos
e rejeita cadeias equivalentes por valor mas compostas por referencias diferentes, exigindo
`ReferenceEquals(ContinuityResult.SequenceComparison, SequenceComparison)` e
`ReferenceEquals(ContinuityPathsResult.ContinuityResult, ContinuityResult)`. Ele nao adiciona
ids, status, fingerprint, confidence agregada, history, ledger, metadata de persistencia,
lifecycle agregado nem aliases. Nenhum comando CLI e nenhum renderer HTML consome esse
analyzer ainda.

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
5. Já existe persistência de snapshot de uma única run (evidência imutável), comparação
   explícita de snapshots, criação de run index ordenado e consumo desse índice para
   travessia adjacente; ainda não existe ledger, history, lifecycle multi-run, `Reopened`
   nem persistent clash ID.

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

Comparar snapshots persistidos pela CLI:

```bash
dotnet run --project src/OrzioClashReport.Cli -- \
  compare-snapshots \
  --previous-snapshot previous-run.json \
  --current-snapshot current-run.json \
  --output comparison.html
```

Regras do contrato:

1. `--previous-snapshot` e `--current-snapshot` são obrigatórios.
2. Previous/current continuam sendo papéis explícitos e nunca são reordenados por
   `CreatedAt`, `RunId`, revisão, nome de ficheiro ou metadata do snapshot.
3. `JsonCoordinationRunSnapshotSerializer.Load` continua sendo a autoridade de parsing e
   validação do snapshot.
4. Matching e lifecycle são sempre recalculados a partir da evidência persistida; não há
   HTML persistido, lifecycle persistido nem derived state persistido no snapshot.
5. Sem `-o`/`--output`, o comando imprime apenas o summary determinístico de 11 linhas.
6. Com `-o`/`--output`, o mesmo summary é seguido por `Comparison report written to ...` e
   pelo HTML revision-aware recém-renderizado.
7. O comando aceita o mesmo snapshot nos dois papéis apenas para smoke sintético.
8. Ainda não há discovery automático de snapshots, nem inferência cronológica, latest /
   previous lookup, lifecycle multi-run, Clash Ledger, `Reopened` ou persistent clash ID.

Criar run index ordenado pela CLI:

```bash
dotnet run --project src/OrzioClashReport.Cli -- \
  index-snapshots \
  --snapshot runs/run-001.json \
  --snapshot runs/run-002.json \
  --output run-index.json
```

Formato do run index:

```json
{
  "schemaVersion": 1,
  "snapshotPaths": [
    "runs/run-001.json",
    "runs/run-002.json"
  ]
}
```

Regras do contrato:

1. `--snapshot` é obrigatório, repetível, preserva ordem e preserva duplicados.
2. `-o`/`--output` é obrigatório.
3. A ordem do índice vem somente da ordem explícita dos argumentos `--snapshot`.
4. O índice persiste apenas referências canónicas relativas ao diretório do próprio
   ficheiro de índice, sempre com separador `/`.
5. `JsonCoordinationRunSnapshotSerializer.Load` continua sendo a autoridade para validar os
   snapshots de entrada; o adapter de run index não desserializa snapshots nem inspeciona
   DTOs deles.
6. Os snapshots continuam sendo a autoridade para a evidência imutável de run.
7. Matching e lifecycle não são persistidos no índice.
8. Ainda não existe discovery automático, inferência cronológica, latest/previous lookup,
   comparação non-adjacent ou all-vs-all, lifecycle multi-run, Clash Ledger, `Reopened`
   ou persistent clash ID.

Limites honestos desta etapa:

- Já existem criação de snapshot, comparação explícita de dois snapshots, criação explícita
  de run index ordenado e consumo explícito desse índice para traversal adjacente; a ordem
  do índice continua sendo a única autoridade de sequência, e todos os snapshots /
  comparações são carregados e calculados antes do primeiro output.
- A travessia adjacente do `compare-index` agora é formalizada no Core por
  `IClashRunSequenceComparer`/`DeterministicAdjacentClashRunSequenceComparer`, que produz um
  `ClashRunSequenceComparisonResult`; o stdout do `compare-index` permanece byte-a-byte igual.
- Ainda não há discovery automático, inferência cronológica, latest/previous lookup,
  comparação non-adjacent, all-vs-all, lifecycle multi-run ou derived state persistido.
- Ainda não há Clash Ledger.
- Ainda não há `Reopened`.
- Ainda não há persistent clash ID.
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
