---
document: "FONTE — Orzio Clash Report"
status: "Fonte canônica de produto, arquitetura e evolução"
language: "pt-BR"
last_verified: "2026-07-29"
repository: "engpaulocosta-bim/orzio-clash-report"
default_branch: "master"
verified_master_commit: "3ea4703dccae9572e74b34913f34d88acd0fb2c9"
verified_release: "v0.1.0-preview.3"
---

# FONTE — ORZIO CLASH REPORT

## 1. Finalidade deste documento

Este arquivo é a **fonte geral de verdade do projeto Orzio Clash Report**.

Ele consolida:

- definição e posicionamento do produto;
- estado atual do motor;
- arquitetura do repositório;
- componentes existentes;
- contratos de entrada e saída;
- comandos e fluxos disponíveis;
- regras de agrupamento, comparação e governança;
- segurança, privacidade e determinismo;
- testes, release e validação;
- limitações reais da versão atual;
- visão da futura aplicação desktop visual;
- requisitos para Windows e macOS;
- regras que não podem ser quebradas durante a evolução.

Este documento deve orientar:

- Codex, Claude Code e outros agentes;
- novos desenvolvedores;
- decisões de arquitetura;
- criação da interface visual;
- criação de instaladores;
- planejamento de versões;
- revisão de Pull Requests;
- documentação operacional e comercial.

Ele não substitui código, testes ou schemas. Em caso de divergência, a autoridade é:

1. código compilado e testes automatizados;
2. adapters e schemas versionados;
3. `AGENTS.md` e `.claude/skills/orzio-clash-report/SKILL.md`;
4. este documento;
5. demais documentos operacionais.

Mudanças estruturais devem atualizar esta FONTE no mesmo Pull Request.

---

# 2. Identidade do produto

## 2.1 Nome

**Orzio Clash Report**

Executável e assembly atuais:

```text
orzioclash
orzioclash.exe
```

Nome técnico histórico:

```text
OrzioClashReport
```

## 2.2 Categoria

Ferramenta BIM para:

- leitura de resultados do Autodesk Navisworks Clash Detective;
- redução de ruído em relatórios de interferências;
- agrupamento determinístico;
- relatórios HTML de coordenação;
- comparação entre revisões;
- snapshots imutáveis;
- análise longitudinal controlada;
- governança explícita de identidade de clashes por decisão humana.

## 2.3 Proposta de valor

O Navisworks já detecta interferências. O Orzio Clash Report transforma os resultados brutos em informação organizada, revisável e comunicável.

```text
Centenas ou milhares de clashes brutos
→ deduplicação honesta
→ grupos técnicos
→ relatório visual
→ comparação entre revisões
→ histórico operacional
→ decisões humanas explícitas
```

O produto busca reduzir:

- repetição;
- ruído;
- tempo de leitura;
- dificuldade de comunicação;
- perda de contexto entre revisões.

E aumentar:

- clareza;
- rastreabilidade;
- consistência;
- qualidade visual;
- velocidade de revisão;
- confiança no resultado.

---

# 3. Estado atual verificado

## 3.1 Release

```text
v0.1.0-preview.3
```

Commit de merge verificado no `master`:

```text
3ea4703dccae9572e74b34913f34d88acd0fb2c9
```

Distribuição atual:

```text
Windows x64
self-contained
single-file
```

Arquivo principal:

```text
orzioclash.exe
```

## 3.2 Validação

```text
1455 testes aprovados
0 failed
0 skipped
0 warnings
0 errors
```

Também foram confirmados:

- build Release;
- publicação `win-x64`;
- pacote ZIP;
- ausência de PDB;
- SHA-256;
- download da release;
- `--version`;
- `--help`;
- `smoke-release.ps1` completo.

Checksum verificado pelo proprietário para o ZIP publicado da preview.3:

```text
3e99e7a315bbbf7ca726c48b7520acc44e0e3f46807f6066736598de627d0719
```

## 3.3 Maturidade real

Descrição correta:

```text
internal controlled pilot
motor CLI funcional
pacote técnico validado
```

Descrições proibidas neste estágio:

```text
produto final
production-ready
enterprise-ready
MVP longitudinal plenamente validado
identidade automática de clashes
substituto da coordenação humana
```

## 3.4 Validação humana

O fluxo single-run foi validado de forma limitada em um export real privado e anonimizado.

Ainda falta validação longitudinal com três exports históricos reais, distintos e sequenciais do mesmo projeto.

Portanto:

- parsing single-run: validado de forma limitada;
- grouping single-run: validado de forma limitada;
- HTML single-run: validado de forma limitada;
- matching longitudinal: experimental;
- lifecycle longitudinal: experimental;
- continuity links/paths: experimentais;
- relatório longitudinal: experimental.

---

# 4. Princípios fundamentais

## 4.1 Evidência, sugestão e decisão

### Evidência

XML, manifest e snapshots representam fatos de uma execução.

### Sugestão

O matcher pode sugerir relação entre um clash anterior e um atual. Sugestão não é verdade persistida.

### Decisão

Identidade persistente só existe quando um humano registra:

```text
ConfirmSameIdentity
```

com:

```text
persistentIdentityId
```

Uma rejeição usa:

```text
RejectSameIdentity
```

sem `persistentIdentityId`.

## 4.2 Imutabilidade

Snapshots são evidência imutável.

O sistema não deve:

- reescrever snapshots;
- editar resultados importados;
- persistir lifecycle como evidência;
- persistir sugestões como verdade.

## 4.3 Determinismo

Com os mesmos inputs e a mesma versão:

- grouping igual;
- ordenação igual;
- JSON byte-identical;
- HTML byte-identical;
- stdout estável.

Não usar em outputs determinísticos:

- `DateTime.Now`;
- GUID aleatório;
- ordem de dicionário não controlada;
- locale variável;
- username;
- paths absolutos;
- diretório atual.

## 4.4 Honestidade

Distinguir sempre:

- compila;
- testes passam;
- smoke passa;
- validado em fixture;
- validado em export real;
- validado longitudinalmente.

## 4.5 Privacidade local

A versão atual não possui:

- cloud sync;
- telemetria;
- upload automático;
- banco remoto;
- autenticação;
- multiusuário.

---

# 5. Arquitetura do repositório

## 5.1 Estilo

```text
Ports and Adapters
Hexagonal Architecture
```

Regra:

```text
Dependências apontam para dentro.
```

O Core não conhece XML, HTML, filesystem, CLI, Navisworks, JSON ou UI.

## 5.2 Solution

```text
OrzioClashReport.sln
├── src/
│   ├── OrzioClashReport.Core/
│   ├── OrzioClashReport.Input.NavisworksXml/
│   ├── OrzioClashReport.Input.RunManifestJson/
│   ├── OrzioClashReport.Output.Html/
│   ├── OrzioClashReport.Persistence.RunSnapshotJson/
│   ├── OrzioClashReport.Persistence.RunIndexJson/
│   ├── OrzioClashReport.Persistence.ProjectCatalogJson/
│   ├── OrzioClashReport.Persistence.IdentityGovernanceJson/
│   └── OrzioClashReport.Cli/
├── tests/
│   └── OrzioClashReport.Tests/
├── samples/
├── scripts/
├── docs/
├── .github/workflows/
├── AGENTS.md
├── README.md
└── CHANGELOG.md
```

## 5.3 Projetos

### Core

```text
netstandard2.0
zero third-party dependencies
nullable enabled
warnings as errors
```

Contém:

- domínio imutável;
- grouping;
- matching;
- comparison;
- lifecycle;
- continuity;
- presentation models;
- validações puras;
- ports.

### Input.NavisworksXml

Responsável por ler XML do Clash Detective e mapear batches, clashes, status, objetos, pontos e campos opcionais.

### Input.RunManifestJson

Carrega manifest versionado, `runId`, `createdAt`, modelos, revisões e testes executados.

### Output.Html

Gera:

- single-run HTML;
- pairwise lifecycle HTML;
- longitudinal HTML;
- identity governance review HTML.

Contratos:

- autocontido;
- CSS embutido;
- escaping;
- LF-only;
- determinismo.

### Persistence.RunSnapshotJson

Persiste e carrega `CoordinationRun` imutável.

### Persistence.RunIndexJson

Persiste sequência explícita de snapshots.

### Persistence.ProjectCatalogJson

Persiste estado operacional mínimo do projeto.

### Persistence.IdentityGovernanceJson

Persiste decisões humanas em schema v1 estrito, UTF-8 sem BOM, LF-only e safe replace.

### CLI

```text
Target: net8.0
Assembly: orzioclash
Version: 0.1.0-preview.3
```

Responsável por argumentos, composição, stdout, stderr e exit code. Não contém regra de negócio.

### Tests

```text
net8.0
xUnit
```

Cobertura inclui parser, domínio, grouping, renderer, CLI, JSON, snapshots, run index, project catalog, matching, lifecycle, continuity, governança, privacidade, packaging e release.

---

# 6. Modelo de domínio

## 6.1 Single-run

Tipos centrais:

```text
ClashReportDocument
ClashBatch
ClashResult
ClashObject
ClashPoint
ClashStatus
GroupedClashReport
ClashGroup
```

`ClashResult` representa um clash bruto com nome, status, distância, grid location, ponto XYZ, elemento A, elemento B e GUID quando disponível.

Status não reconhecido deve virar:

```text
Unknown
```

## 6.2 Revisões e runs

```text
ModelIdentity
ModelRevision
RunManifest
ExecutedClashTest
ClashOccurrence
CoordinationRun
```

`ModelIdentity` é estável e não contém revisão, path, hash, run id ou timestamp.

`ModelRevision` combina identidade estável com metadados de revisão.

`ClashOccurrence` é evidência de um run, não identidade cross-run.

`CoordinationRun` é:

```text
RunManifest
+
lista ordenada de ClashOccurrence
```

## 6.3 Matching

Conceitos:

```text
ClashMatchAssessment
ClashMatchConfidence
MatchEvidence
```

Confianças:

```text
Low
Medium
High
```

`High` não significa confirmação humana.

Evidência pode ser:

```text
Supports
Contradicts
Unavailable
```

## 6.4 Lifecycle

Estados derivados conceituais:

```text
New
Persistent
Resolved
```

São recalculáveis e não são evidência persistida.

## 6.5 Continuidade

O motor pode projetar links, paths e summaries em transições adjacentes. Esta parte permanece experimental.

---

# 7. Parsing do Navisworks XML

Input oficial atual:

```text
Clash Detective XML export
```

A ferramenta não lê diretamente NWD, NWF, NWC, RVT, ACC, BIM 360 ou API live.

Regras do parser:

- ler apenas membros verificados;
- campos opcionais permanecem opcionais;
- input malformado falha claramente;
- ordem A/B é preservada;
- revisão não é inferida;
- identidade persistente não é inferida;
- erros não são engolidos.

A arquitetura reserva um futuro adapter live do Navisworks sem mudar o Core.

---

# 8. Agrupamento single-run

## 8.1 Pipeline

```text
clashes brutos
→ deduplicação
→ resolução de disciplina
→ nível
→ bucket técnico
→ ordenação determinística
```

## 8.2 Deduplicação

Duplicatas:

```text
mesmo par não ordenado de element ids
+
pontos dentro da tolerância
```

## 8.3 Bucket

```text
clash test
+
par de disciplinas independente de ordem
+
nível combinado
```

Clashes de testes diferentes nunca são misturados.

## 8.4 Ordenação

1. clash test;
2. disciplina A;
3. disciplina B;
4. nível.

## 8.5 Disciplina

Resolvida por:

```text
IDisciplineResolver
```

Não deve ser hardcoded na UI, grouper ou renderer.

---

# 9. HTML single-run

Características:

- arquivo único;
- offline;
- CSS embutido;
- sem JavaScript externo;
- light theme;
- escaping;
- deterministic byte output.

Conteúdo atual:

- fonte;
- raw count;
- group count;
- badges de disciplina;
- nível;
- clash test;
- quantidade;
- Name;
- Status;
- Distance;
- Point;
- Element A;
- Element B.

Direção futura:

- identidade premium;
- resumo executivo;
- KPIs;
- filtros;
- gráficos;
- cards;
- impressão A4/A3;
- templates;
- logo do cliente;
- idioma;
- PDF futuro;
- viewpoints futuros.

A apresentação pode evoluir sem alterar o cálculo.

---

# 10. Manifest schema v2

Exemplo:

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
      "sourceFileName": "Sigma_Structure_R04.nwc",
      "sourceFilePath": "C:\\BIM\\SyntheticProject\\Models\\Sigma_Structure_R04.nwc"
    }
  ],
  "executedClashTests": [
    {
      "name": "Structure vs Architecture",
      "modelA": {
        "company": "Sigma",
        "discipline": "Structure",
        "modelName": "Sigma_Structure"
      },
      "modelB": {
        "company": "Beta",
        "discipline": "Architecture",
        "modelName": "Beta_Architecture"
      }
    }
  ]
}
```

Regras:

- um manifest por run;
- `runId` explícito;
- `createdAt` explícito;
- revisões explícitas;
- testes executados explícitos;
- nenhuma inferência por filename;
- revisão humana obrigatória.

Na futura aplicação visual, o manifest será criado por formulário, não por edição manual de JSON.

---

# 11. Snapshots

Entrada:

```text
XML + manifest
```

Saída:

```text
CoordinationRun JSON
```

Regras:

- create-new;
- occurrence order preservada;
- revisions validadas;
- sem matching;
- sem lifecycle;
- sem identidade persistente.

---

# 12. Run index

Schema:

```json
{
  "schemaVersion": 1,
  "snapshotPaths": [
    "snapshots/run-001.json",
    "snapshots/run-002.json",
    "snapshots/run-003.json"
  ]
}
```

Regra principal:

```text
A ordem de snapshotPaths é a única autoridade de sequência.
```

Não existe ordenação automática por timestamp, filename, revision ou run id.

`compare-index` compara apenas pares adjacentes.

---

# 13. Project catalog

Conteúdo:

```text
projectId
displayName
runIndexPath
longitudinalReportPath
```

Não contém snapshots, matching, lifecycle, continuity, governance, identidade persistente, Clash Ledger ou Reopened.

As referências são relativas e usam `/`.

A árvore pode ser movida em conjunto.

---

# 14. Identity Governance

Tipos:

```text
ConfirmSameIdentity
RejectSameIdentity
```

Cada decisão usa dois endpoints:

```text
runId
occurrenceIndex
```

Confirmação exige `persistentIdentityId`.

Rejeição proíbe `persistentIdentityId`.

Proveniência usa `reviewerAlias`, sem exigir nome real, email, login ou timestamp automático.

A validação verifica somente:

- project id;
- run único existente;
- occurrence index existente.

Não verifica matcher, adjacência, transitivity, graph conflicts, reviewer identity ou responsabilidade.

O review report:

- exige validação;
- é derivado;
- não altera inputs;
- preserva ordem;
- preserva Left/Right;
- não mostra raw `ClashObject.SourceModel`;
- não agrupa por persistent identity.

---

# 15. Catálogo de comandos CLI

## Global

```powershell
.\orzioclash.exe --version
.\orzioclash.exe --help
```

## Single-run

```powershell
.\orzioclash.exe "<input.xml>" -o "<output.html>"
```

## Compare

```powershell
.\orzioclash.exe compare `
  --previous-xml "<previous.xml>" `
  --previous-manifest "<previous.json>" `
  --current-xml "<current.xml>" `
  --current-manifest "<current.json>" `
  -o "<comparison.html>"
```

## Snapshot

```powershell
.\orzioclash.exe snapshot `
  --xml "<input.xml>" `
  --manifest "<manifest.json>" `
  -o "<snapshot.json>"
```

## Compare snapshots

```powershell
.\orzioclash.exe compare-snapshots `
  --previous-snapshot "<previous.json>" `
  --current-snapshot "<current.json>" `
  -o "<comparison.html>"
```

## Index snapshots

```powershell
.\orzioclash.exe index-snapshots `
  --snapshot "<run-001.json>" `
  --snapshot "<run-002.json>" `
  -o "<run-index.json>"
```

## Compare index

```powershell
.\orzioclash.exe compare-index `
  --index "<run-index.json>" `
  -o "<longitudinal.html>"
```

## Create project

```powershell
.\orzioclash.exe create-project `
  --project-id "<project-id>" `
  --name "<display name>" `
  --index "<run-index.json>" `
  --report "<longitudinal.html>" `
  -o "<project.json>"
```

## Append project snapshot

```powershell
.\orzioclash.exe append-project-snapshot `
  --project "<project.json>" `
  --snapshot "<run-004.json>"
```

## Render project

```powershell
.\orzioclash.exe render-project --project "<project.json>"
```

## Create governance

```powershell
.\orzioclash.exe create-identity-governance `
  --project-id "<project-id>" `
  -o "<identity-governance.json>"
```

## Append confirmation

```powershell
.\orzioclash.exe append-identity-decision `
  --governance "<identity-governance.json>" `
  --decision-id "<decision-id>" `
  --decision-kind ConfirmSameIdentity `
  --left-run-id "<run-id>" `
  --left-occurrence-index <index> `
  --right-run-id "<run-id>" `
  --right-occurrence-index <index> `
  --persistent-identity-id "<identity-id>" `
  --reviewer-alias "<alias>" `
  --reason "<reason>"
```

## Append rejection

```powershell
.\orzioclash.exe append-identity-decision `
  --governance "<identity-governance.json>" `
  --decision-id "<decision-id>" `
  --decision-kind RejectSameIdentity `
  --left-run-id "<run-id>" `
  --left-occurrence-index <index> `
  --right-run-id "<run-id>" `
  --right-occurrence-index <index> `
  --reviewer-alias "<alias>" `
  --reason "<reason>"
```

## Validate governance

```powershell
.\orzioclash.exe validate-identity-governance `
  --project "<project.json>" `
  --governance "<identity-governance.json>"
```

## Render governance review

```powershell
.\orzioclash.exe render-identity-governance-report `
  --project "<project.json>" `
  --governance "<identity-governance.json>" `
  -o "<identity-governance-review.html>"
```

---

# 16. Persistência segura

## Create-new

Outputs que representam nova evidência não são sobrescritos silenciosamente.

## Safe replace

1. validar;
2. serializar completamente;
3. escrever temporário no mesmo diretório;
4. concluir escrita;
5. substituir destino;
6. limpar temporário;
7. preservar original em falha.

Temporários conhecidos:

```text
.identity-governance-replace-*.tmp
.run-index-replace-*.tmp
.derived-html-report-*.tmp
```

---

# 17. Segurança e privacidade

O sistema valida colisões entre output e:

- project catalog;
- run index;
- snapshots;
- governance;
- relatório longitudinal.

O review report não projeta raw `ClashObject.SourceModel`, evitando paths Windows, UNC, Linux e estruturas privadas de cliente.

Não incluir em Git ou release:

- XML real;
- HTML real;
- NWD/NWF/NWC/RVT;
- screenshots;
- PDFs;
- nomes reais;
- paths privados;
- dados pessoais.

---

# 18. Release e packaging

Workflow:

```text
.github/workflows/release.yml
```

Gatilhos:

```text
workflow_dispatch
push de tag v*
```

`workflow_dispatch` é dry run. Publicação ocorre em tag.

O workflow faz restore, build, test, publish, smoke, valida versão/tag/master, monta pacote, confere lista exata, rejeita PDB e arquivos proibidos, valida ZIP, gera checksum e publica prerelease.

Conteúdo preview.3:

```text
orzioclash.exe
README.md
CHANGELOG.md
smoke-release.ps1
docs/operations/internal-preview.md
docs/operations/project-catalog.md
docs/operations/release-checklist.md
docs/operations/identity-governance-cli.md
docs/operations/identity-governance-validation.md
docs/operations/identity-governance-review-report.md
docs/operations/pilot-evaluation.md
samples/sample-clash.xml
samples/sample-clash.run-manifest.json
samples/run-manifest.sample.json
samples/run-index.template.json
```

---

# 19. Uso atual e limitação de experiência

A CLI atual é adequada para:

- desenvolvedor;
- BIM Manager técnico;
- BIM Coordinator com familiaridade em terminal;
- avaliador interno acompanhado.

Não é adequada para:

- utilizador comercial comum;
- coordenador que espera instalar e clicar;
- empresa sem apoio técnico.

Hoje o utilizador precisa compreender terminal, paths, argumentos, JSON, manifests, run ids, occurrence indexes e ordem de comandos.

O motor está pronto. A experiência visual de produto ainda não está pronta.

---

# 20. Visão futura: Orzio Clash Report Desktop

## 20.1 Objetivo

Criar uma aplicação desktop visual completa, instalável e estável para:

```text
Windows
macOS
```

Nome de trabalho:

```text
Orzio Clash Report Desktop
```

Não será apenas um launcher mínimo. Será uma solução completa para preparação, geração, revisão e exportação de relatórios de clash detection com alta qualidade visual e precisão.

## 20.2 Princípio

```text
Não reescrever o motor.
Criar experiência e application layer sobre o motor existente.
```

A CLI permanece para automação, CI, diagnóstico e utilizadores avançados.

A UI deverá usar serviços diretamente, sem depender permanentemente de chamar a CLI como subprocesso.

## 20.3 Experiência desejada

O utilizador deverá:

1. instalar;
2. abrir;
3. criar projeto;
4. arrastar XML;
5. revisar dados do run;
6. gerar relatório;
7. abrir preview;
8. exportar;
9. adicionar nova revisão;
10. comparar;
11. rever decisões;
12. salvar workspace.

Sem PowerShell ou edição de JSON.

---

# 21. Arquitetura futura da aplicação visual

```text
Orzio Clash Report Desktop
├── UI
├── Application
├── Core
├── Input Adapters
├── Persistence Adapters
├── Output Adapters
└── Platform Adapters
```

## Application layer

Criar futuramente:

```text
OrzioClashReport.Application
```

Casos de uso:

- CreateSingleRunReport;
- CreateSnapshot;
- CreateRunIndex;
- CompareRuns;
- CreateProject;
- AppendProjectRun;
- RenderProject;
- CreateGovernance;
- AppendDecision;
- ValidateGovernance;
- RenderGovernanceReview.

A UI não deve montar pipelines nem conter regra de negócio.

## Framework de UI

A escolha deve permanecer aberta até um spike técnico.

Critérios:

- Windows;
- macOS;
- acessibilidade;
- file picker;
- drag-and-drop;
- HTML preview;
- impressão;
- performance;
- packaging;
- assinatura;
- notarização;
- manutenção;
- compatibilidade com o Core.

## Platform adapters

```text
IFilePicker
IFolderPicker
IReportPreview
IExternalBrowserLauncher
IRecentProjectsStore
IUserSettingsStore
IClipboardService
IPlatformPathService
IApplicationUpdateService
```

## Windows

Futuro:

- instalador;
- atalho;
- ícone;
- desinstalador;
- code signing;
- SmartScreen strategy;
- logs;
- atualização.

## macOS

Futuro:

- `.app`;
- Apple Silicon;
- Intel ou universal;
- assinatura;
- notarização;
- entitlements;
- DMG ou PKG;
- Gatekeeper validation.

---

# 22. Experiência visual proposta

## Home

- Novo projeto;
- Abrir projeto;
- Relatório rápido;
- Projetos recentes;
- Ajuda;
- Configurações.

## Relatório rápido

1. selecionar ou arrastar XML;
2. escolher destino;
3. escolher template;
4. gerar;
5. visualizar;
6. exportar;
7. abrir pasta.

## Novo projeto

Wizard:

- nome;
- project id;
- pasta;
- idioma;
- template;
- configurações.

Estrutura automática:

```text
project/
├── project.json
├── identity-governance.json
├── run-index.json
├── inputs/
├── manifests/
├── snapshots/
├── reports/
└── attachments/
```

## Import run

Campos visuais:

- XML;
- run id;
- data/hora;
- modelos;
- empresa;
- disciplina;
- revisão;
- source file;
- testes executados.

## Timeline

- runs;
- datas;
- revisões;
- raw clashes;
- groups;
- status;
- reports;
- warnings.

## Longitudinal

- transições;
- filtros;
- pesquisa;
- detalhes;
- export HTML;
- PDF futuro.

## Governança visual

1. selecionar Left;
2. selecionar Right;
3. visualizar contexto;
4. confirmar/rejeitar;
5. persistent identity quando aplicável;
6. alias;
7. motivo;
8. validar;
9. salvar;
10. gerar review.

## Report Studio

- logo Orzio;
- logo cliente;
- capa;
- cores;
- tipografia;
- idioma;
- colunas;
- resumo executivo;
- gráficos;
- impressão;
- rodapé;
- templates.

Configuração visual não altera resultado técnico.

---

# 23. Sistema visual e precisão

Objetivo visual:

```text
premium
técnico
limpo
corporativo
legível
consistente
```

Não depender somente de cor. Combinar texto, label, ícone e cor.

A UI deve diferenciar claramente:

```text
Evidence
Suggested
Matched
Derived
Human Confirmed
Human Rejected
Experimental
```

Warnings obrigatórios:

- manifest incompleto;
- model mapping ambíguo;
- status desconhecido;
- duplicate run id;
- occurrence inválido;
- project mismatch;
- output collision;
- run order duvidosa;
- funcionalidade experimental.

---

# 24. Logs, suporte e diagnóstico

Logs locais redigidos.

Não registrar por padrão XML completo, paths privados, credenciais, conteúdo de modelos ou dados pessoais.

Diagnóstico futuro:

- versão;
- OS;
- arquitetura;
- caso de uso;
- código de erro;
- mensagem redigida;
- passos;
- confirmação de anonimização.

---

# 25. Instalação, atualização e schemas

## Instalação

Entregar:

```text
instalador
atalho
aplicação
documentação inicial
samples
```

## First run

1. idioma;
2. pasta default;
3. política de dados;
4. projeto ou relatório rápido;
5. tutorial.

## Atualização

- não alterar dados;
- verificar assinatura;
- mostrar versão;
- permitir adiar;
- manter schemas;
- backup antes de migração.

## Schemas

Nunca mudar silenciosamente.

Toda mudança exige:

- nova `schemaVersion`;
- loader compatível;
- migração explícita;
- testes;
- documentação;
- backup/rollback.

---

# 26. Fora de escopo atual

- Clash Ledger;
- Reopened;
- identity propagation;
- transitivity;
- graph merge;
- project-wide identity graph;
- automatic chronology;
- automatic responsibility;
- comments colaborativos;
- banco de dados;
- multiusuário;
- cloud sync;
- autenticação;
- ACC/BIM 360;
- telemetria;
- live Navisworks API;
- viewpoints;
- imagens;
- PDF;
- licenciamento;
- pricing;
- checkout;
- trial enforcement;
- auto-update;
- code signing;
- macOS package.

Abrir um front por vez.

---

# 27. Roadmap recomendado

## Fase 32 — Fundação visual

- Application layer;
- contratos da UI;
- spike de stack desktop;
- shell Windows/macOS;
- projeto;
- relatório rápido;
- preview HTML;
- logs;
- testes.

## Fase 33 — Projeto visual completo

- wizard;
- editor de manifest;
- import de runs;
- snapshots;
- run index;
- timeline;
- render project.

## Fase 34 — Relatórios premium

- report studio;
- templates;
- branding;
- filtros;
- KPIs;
- gráficos;
- impressão.

## Fase 35 — Governança visual

- seleção Left/Right;
- contexto;
- confirmação;
- rejeição;
- validation;
- review.

## Fase 36 — Distribuição desktop

- installer Windows;
- app macOS;
- assinatura;
- notarização;
- atualização;
- smoke cross-platform.

## Fase 37 — Piloto visual

- avaliadores reais;
- três exports históricos reais;
- feedback;
- correções;
- decisão comercial.

---

# 28. Critérios de aceitação da aplicação visual

## Funcional

Sem terminal, o utilizador consegue:

- single-run report;
- criar projeto;
- importar três runs;
- editar manifests;
- criar snapshots;
- criar índice;
- gerar longitudinal;
- criar governance;
- confirmar/rejeitar;
- validar;
- gerar review.

## Técnico

- Core preservado;
- zero lógica na UI;
- determinismo;
- schemas preservados;
- CLI preservada;
- testes verdes;
- smoke Windows e macOS.

## Visual

- layout consistente;
- legibilidade;
- estados claros;
- preview fiel;
- impressão aceitável;
- nenhum path privado.

## Operacional

- instalação;
- desinstalação;
- projeto portátil;
- backup;
- logs;
- recuperação;
- documentação;
- sem PowerShell.

---

# 29. Regras para desenvolvimento futuro

1. Um front por vez.
2. Não reescrever o Core por causa da UI.
3. Não colocar grouping na UI.
4. Não colocar parser XML na UI.
5. Não mudar schema sem versão.
6. Não persistir sugestão como verdade.
7. Não apresentar `High` como confirmação humana.
8. Não inferir run order.
9. Não alterar snapshots.
10. Não expor raw paths.
11. Não introduzir cloud sem decisão.
12. Não introduzir licenciamento sem decisão jurídica.
13. Não remover CLI.
14. Não quebrar contratos publicados silenciosamente.
15. Não reduzir cobertura.
16. Não confundir smoke com validação real.
17. Não coletar dados privados por padrão.
18. Não misturar apresentação e cálculo.
19. Tratar Windows e macOS como requisitos desde o início.
20. Atualizar esta FONTE em mudanças estruturais.

---

# 30. Fontes do repositório

Documentos:

```text
README.md
AGENTS.md
.claude/skills/orzio-clash-report/SKILL.md
CHANGELOG.md
docs/architecture/identity-governance.md
docs/operations/internal-preview.md
docs/operations/project-catalog.md
docs/operations/identity-governance-cli.md
docs/operations/identity-governance-validation.md
docs/operations/identity-governance-review-report.md
docs/operations/pilot-evaluation.md
docs/operations/release-checklist.md
```

Código:

```text
src/OrzioClashReport.Core/
src/OrzioClashReport.Input.NavisworksXml/
src/OrzioClashReport.Input.RunManifestJson/
src/OrzioClashReport.Output.Html/
src/OrzioClashReport.Persistence.RunSnapshotJson/
src/OrzioClashReport.Persistence.RunIndexJson/
src/OrzioClashReport.Persistence.ProjectCatalogJson/
src/OrzioClashReport.Persistence.IdentityGovernanceJson/
src/OrzioClashReport.Cli/
```

Validação:

```text
tests/OrzioClashReport.Tests/
scripts/smoke-release.ps1
.github/workflows/ci.yml
.github/workflows/release.yml
```

Fixtures:

```text
samples/sample-clash.xml
samples/sample-clash.run-manifest.json
samples/run-manifest.sample.json
samples/run-index.template.json
```

---

# 31. Decisões pendentes

## Produto

- nome final desktop;
- posicionamento;
- público;
- preço;
- licença;
- trial;
- suporte;
- updates.

## Tecnologia

- framework UI;
- HTML preview;
- PDF;
- packaging macOS;
- builds universal/separados;
- installer Windows;
- assinatura;
- notarização.

## Domínio

- validação longitudinal real;
- Clash Ledger;
- Reopened;
- identity graph;
- responsibility;
- chronology;
- colaboração.

## Jurídico

Ainda sem decisão final documentada para LICENSE, EULA, termos comerciais, redistribuição, garantia e responsabilidade.

Até decisão explícita:

```text
distribuição privada e autorizada pelo proprietário
```

---

# 32. Resumo executivo

O Orzio Clash Report já possui um motor técnico funcional e testado para:

- ler XML do Navisworks;
- deduplicar;
- agrupar;
- gerar HTML;
- criar snapshots;
- comparar revisões;
- manter ordem explícita;
- criar projeto operacional;
- registrar decisões humanas;
- validar evidências;
- gerar review report.

A `v0.1.0-preview.3` prova que o motor, o pacote Windows, o executável, o smoke e o workflow de release funcionam.

O próximo trabalho principal não é recriar o motor. É transformá-lo em um produto desktop visual completo:

```text
instalável
cross-platform
fácil de usar
visualmente premium
tecnicamente honesto
preciso
seguro
offline-first
```

O motor existente é a fundação. A aplicação visual será a camada de produto.

---

# 33. Declaração canônica

```text
Orzio Clash Report é uma ferramenta BIM local e determinística que transforma exports do
Navisworks Clash Detective em relatórios técnicos organizados, comparações revision-aware
e decisões humanas explícitas, preservando a evidência original e separando claramente
fato, sugestão algorítmica e decisão humana.

A versão atual entrega o motor CLI e o pacote técnico para Windows. A próxima grande fase
será a construção de uma aplicação desktop visual completa e instalável para Windows e
macOS, sem reescrever ou enfraquecer o motor já validado.
```
