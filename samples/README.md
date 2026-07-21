# Samples

The `sample-clash.xml` and `sample-clash2.xml` fixtures are Navisworks Clash Detective XML
exports with synthetic, anonymized data. Real project names, company names, network paths,
and file names were replaced with fictitious values. The XML structure, element counts, and
relationships needed by parsing and grouping tests were preserved.

Before adding any new fixture from a real export, anonymize client names, company names,
project names, absolute paths, drive letters, network paths, and real file names.

`run-manifest.sample.json` is a separate fully synthetic fixture. It manually declares
three models and revisions for a hypothetical coordination run using schema version 2
(`schemaVersion: 2`). The manifest is an explicit declaration: model revisions are never
inferred from file names, paths, or XML. It also declares three `executedClashTests`
entries, including a third test ("Architecture vs Piping") that illustrates zero-result
coverage: a declared executed test may have no corresponding occurrence, proving that the
test ran and returned zero clashes rather than never running. This sample manifest is not
bound to any `sample-clash*.xml` file or CLI command.

## `sample-clash.run-manifest.json`

This is the companion manifest for the `sample-clash.xml` fixture, used by integration
tests for `ExactSourceModelCoordinationRunAssembler`.

- It applies only to `sample-clash.xml`, not `sample-clash2.xml`.
- Declared models are synthetic and manual. `company`, `discipline`, `modelName`, and
  `revision` are convenience values chosen by hand; no code derives those fields from file
  names.
- The first model's `sourceFileName` (`"Project_A_HVAC_PD_R00.rvt"`) exactly matches the
  `ClashObject.SourceModel` token produced by `NavisworksXmlClashSource` for every clash in
  the fixture. Runtime binding is exact through `ExactSourceModelCoordinationRunAssembler`.
- The five clashes in `"Teste 01"` resolve to the same `ModelRevision` on both sides,
  representing a self-clash scenario. The manifest therefore declares a self-clash
  `ExecutedClashTest` for `"Teste 01"`.
- The second synthetic model (`Beta_Architecture_R10.nwc`) and the "Synthetic Zero-Result
  Test" declaration are illustrative only. They demonstrate a valid executed test with no
  matching occurrence.

## `run-index.template.json`

`run-index.template.json` is a minimal schema-version 1 run-index template with exactly
three illustrative relative snapshot references. It contains no matching, lifecycle,
history, or derived state.
