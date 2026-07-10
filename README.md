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

## Backlog (fora do MVP)

Esta seção existe para registrar pedidos que não entram no MVP.

- Imagens de clash embutidas no relatório
- Exportação em PDF
- Licenciamento
- UI WPF
- Adaptador da API do Navisworks (.NET API, leitura ao vivo)
- Edição de status do clash dentro da ferramenta
- Integração com CDE (Common Data Environment)
