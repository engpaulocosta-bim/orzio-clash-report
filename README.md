# OrzioClashReport

Lê clashes exportados do Navisworks Clash Detective e gera um relatório de coordenação em HTML, agrupado por par de disciplinas e por clash test.

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

### Resolução de disciplina

O agrupamento por par de disciplinas usa `PathHierarchyDisciplineResolver` (em
`OrzioClashReport.Core`) como heurística padrão: tenta o nome do modelo NWD aninhado
(via `pathlink` do export) e cai para a propriedade `Item Source File Name` quando
ausente. Como a nomenclatura de disciplina varia por projeto, essa é uma implementação
plugável de `IDisciplineResolver` — troque por outra se a heurística não bater com as
convenções do seu projeto.

## Backlog (fora do MVP)

Esta seção existe para registrar pedidos que não entram no MVP.

- Imagens de clash embutidas no relatório
- Exportação em PDF
- Licenciamento
- UI WPF
- Adaptador da API do Navisworks (.NET API, leitura ao vivo)
- Edição de status do clash dentro da ferramenta
- Integração com CDE (Common Data Environment)
