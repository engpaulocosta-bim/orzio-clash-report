# OrzioClashReport

Lê clashes exportados do Navisworks Clash Detective e gera um relatório de coordenação em
HTML, agrupado por clash test, par de disciplinas e nível.

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
  `samples/`.
- **Validado em modelo real**: ainda não. Esta validação só pode ser feita por um humano,
  rodando a ferramenta contra um export real (anonimizado) do Clash Detective e conferindo
  se o relatório agrupado corresponde à realidade do projeto.

## CI

`.github/workflows/ci.yml` roda `dotnet build` e `dotnet test` em Release a cada push e pull
request, usando o SDK fixado em `global.json`.

## Run manifest

O manifesto de rodada (`RunManifest`) é uma declaração explícita e auditável de quais
modelos e revisões participaram de uma rodada de coordenação. Ele não é inferido a partir
de nome de arquivo, caminho, XML do Navisworks, Autodesk Forma ou ACC — nesta etapa do
projeto, essa informação é sempre declarada manualmente.

Exemplo (`samples/run-manifest.sample.json`):

```json
{
  "schemaVersion": 1,
  "runId": "coordination-2026-07-10-0900",
  "createdAt": "2026-07-10T09:00:00+01:00",
  "models": [
    {
      "company": "Sigma",
      "discipline": "Structure",
      "modelName": "Sigma_Structure",
      "revision": "R04",
      "sourceFileName": "Sigma_Structure_R04.nwc"
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

O parser (`OrzioClashReport.Input.RunManifestJson`) valida a estrutura e o schema do JSON e
constrói `RunManifest`/`ModelRevision`/`ModelIdentity` do Core. **A CLI ainda não consome o
manifesto nesta etapa** — este é apenas o contrato de entrada, isolado no Core e em um
adapter dedicado.

## Coordination run snapshot

1. `RunManifest` declara quais revisões de modelo participam de uma rodada (ver seção
   acima).
2. `ClashOccurrence` vincula um `ClashResult` bruto (do XML) às revisões exatas dos modelos
   dos lados A e B dentro de um clash test específico.
3. `CoordinationRun` forma o snapshot imutável: o `RunManifest` mais a lista ordenada de
   `ClashOccurrence`s observadas. Toda revisão usada por uma ocorrência precisa estar
   declarada exatamente no manifesto (mesma `ModelIdentity` com revisão diferente é
   rejeitada).

Nenhum matching ou comparação entre rodadas existe ainda — `CoordinationRun` é só o
snapshot de uma rodada isolada. A associação automática entre elementos do XML e as
revisões do manifesto (`ClashObject.SourceModel` → `ModelRevision`) também ainda não foi
implementada; hoje `ClashOccurrence` é construída explicitamente. A CLI ainda não cria esse
snapshot.

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
6. A CLI ainda não usa esses contratos.

## Pairwise matcher port

1. `IClashMatcher` (`src/OrzioClashReport.Core/Abstractions/IClashMatcher.cs`) avalia um par
   ordenado anterior/atual de `ClashOccurrence` — nada mais.
2. Um retorno não nulo (`ClashMatchAssessment`) é uma avaliação candidata, com confiança e
   evidências auditáveis.
3. `null` significa que o matcher não produziu nenhum candidato para aquele par (evidência
   insuficiente, sinais incompatíveis, ou pré-condição da estratégia não satisfeita).
4. `Low` **não** equivale a `null` — `Low` é uma avaliação candidata real, com pelo menos uma
   evidência; `null` é a ausência de avaliação.
5. O port não implementa nenhum algoritmo de matching — nesta etapa não existe nenhuma
   implementação concreta em produção, só fakes de teste.
6. O port não compara rodadas (`CoordinationRun`) completas nem recebe listas de ocorrências.
7. Seleção um-para-um entre candidatos concorrentes, resolução de conflitos e qualquer
   lifecycle status ainda não existem — ficam para um futuro run comparer.
8. A CLI ainda não usa o matcher.

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
8. Ainda não existe comparação entre rodadas completas nem lifecycle status — isso pertence
   a um futuro run comparer.
9. A CLI ainda não usa este matcher.

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
10. A CLI ainda não usa este comparer.

## Backlog (fora do MVP)

Esta seção existe para registrar pedidos que não entram no MVP.

- Imagens de clash embutidas no relatório
- Exportação em PDF
- Licenciamento
- UI WPF
- Adaptador da API do Navisworks (.NET API, leitura ao vivo)
- Edição de status do clash dentro da ferramenta
- Integração com CDE (Common Data Environment)
