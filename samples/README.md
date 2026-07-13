Os fixtures `sample-clash.xml` e `sample-clash2.xml` são exports do Navisworks Clash
Detective com dados sintéticos/anonimizados (nomes de projeto, empresa, caminhos de rede e
nomes de ficheiro foram substituídos por valores fictícios). A estrutura do XML, contagem de
elementos e relações necessárias para parsing/grouping foram preservadas.

Se for adicionar um novo fixture a partir de um export real, anonimize nomes de cliente,
empresa, projeto, caminhos absolutos, letras de rede e nomes de ficheiro reais antes de
versionar o ficheiro.

`run-manifest.sample.json` é um fixture separado e totalmente sintético: declara manualmente
três modelos/revisões de uma rodada de coordenação hipotética, no schema v2
(`schemaVersion: 2`). O manifesto é uma declaração explícita — a revisão de cada modelo
nunca é inferida a partir do nome do arquivo, do caminho ou de qualquer XML. Ele também
declara explicitamente três `executedClashTests` (par de disciplinas + par de modelos
revision-free), incluindo um terceiro test ("Architecture vs Piping") que ilustra a
funcionalidade principal da Etapa 10: uma declaração de cobertura é válida mesmo sem nenhuma
occurrence correspondente no fixture — é assim que se prova que um clash test rodou e
retornou zero clashes, em vez de nunca ter rodado. Ele não está vinculado a nenhum
`sample-clash*.xml` nem à CLI.

## `sample-clash.run-manifest.json`

Este é o companion manifest do fixture XML real `sample-clash.xml`, usado pelos testes de
integração de `ExactSourceModelCoordinationRunAssembler`
(`tests/OrzioClashReport.Tests/ExactSourceModelCoordinationRunAssemblerTests.cs`).

- Acompanha exclusivamente `sample-clash.xml` (não `sample-clash2.xml`, que é um fixture
  não relacionado, usado só por `ParsingLargeSampleTests.cs`).
- Os modelos declarados são **sintéticos/manuais**: `company`, `discipline`, `modelName` e
  `revision` são valores de conveniência escolhidos por quem editou o arquivo à mão — nada
  no código deriva esses campos do nome do arquivo.
- `sourceFileName` do primeiro modelo (`"Project_A_HVAC_PD_R00.rvt"`) foi declarado para
  corresponder **exatamente** ao valor que `NavisworksXmlClashSource` já produz em
  `ClashObject.SourceModel` para todo clash do fixture (via a precedência
  `Item Source File` → `Item Source File Name`, corrigida pelo hotfix
  `498d97a0d56250616f3b9891a9a3a3a805418825`). Nenhuma informação é inferida em runtime: a igualdade é exata, aplicada
  por `ExactSourceModelCoordinationRunAssembler`.
- Os cinco clashes de `"Teste 01"` no fixture resolvem, nos dois lados, para o **mesmo**
  `ModelRevision` — um cenário de self-clash real. Por isso o manifesto declara um
  `ExecutedClashTest` self-clash (`modelA == modelB`) para `"Teste 01"`.
- O segundo modelo (`Beta_Architecture_R10.nwc`) e o `ExecutedClashTest` "Synthetic
  Zero-Result Test" que o referencia **não correspondem a nada no fixture real** — são
  puramente ilustrativos da semântica de zero-result da Etapa 10 (um clash test declarado
  como executado sem nenhuma `ClashOccurrence` correspondente é válido). Nenhum teste de
  integração depende deles.
