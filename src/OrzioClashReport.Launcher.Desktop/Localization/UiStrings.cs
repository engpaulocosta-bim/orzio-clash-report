using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace OrzioClashReport.Launcher.Desktop.Localization
{
    /// <summary>One piece of interface text in both languages.</summary>
    public readonly struct UiText
    {
        public UiText(string portuguese, string english)
        {
            Portuguese = portuguese;
            English = english;
        }

        public string Portuguese { get; }

        public string English { get; }
    }

    /// <summary>
    /// Every piece of interface text, in Portuguese and English. This is the only place a visible
    /// string is written. Values the engine receives are never in here: they are canonical and are
    /// never translated.
    /// </summary>
    public static class UiStrings
    {
        public static IReadOnlyDictionary<string, UiText> All { get; } = Build();

        private static IReadOnlyDictionary<string, UiText> Build()
        {
            var text = new Dictionary<string, UiText>(StringComparer.Ordinal);

            void Add(string key, string portuguese, string english) =>
                text.Add(key, new UiText(portuguese, english));

            // ---- Application and shell -----------------------------------------------------
            Add("App.Title", "Orzio Clash Report", "Orzio Clash Report");
            Add("App.Tagline",
                "Transforme exports do Navisworks Clash Detective em relatórios de coordenação, sem terminal.",
                "Turn Navisworks Clash Detective exports into coordination reports, without a terminal.");
            Add("Shell.Brand", "Orzio", "Orzio");
            Add("Shell.Ready", "Pronto.", "Ready.");

            Add("Nav.Home", "Início", "Home");
            Add("Nav.QuickReport", "Relatório rápido", "Quick report");
            Add("Nav.Snapshots", "Snapshots", "Snapshots");
            Add("Nav.Longitudinal", "Longitudinal", "Longitudinal");
            Add("Nav.Projects", "Projetos", "Projects");
            Add("Nav.Governance", "Governança", "Governance");
            Add("Nav.Settings", "Definições", "Settings");

            // ---- Engine state ---------------------------------------------------------------
            Add("Engine.Label.Checking", "A verificar", "Checking");
            Add("Engine.Label.Ready", "Motor pronto", "Engine ready");
            Add("Engine.Label.VersionMismatch", "Versão diferente", "Version mismatch");
            Add("Engine.Label.IntegrityFailure", "Integridade falhou", "Integrity failed");
            Add("Engine.Label.Missing", "Motor em falta", "Engine missing");
            Add("Engine.Label.Unsupported", "Motor não suportado", "Engine unsupported");

            Add("Engine.Description.Checking",
                "A verificar o motor instalado.",
                "Checking the installed engine.");
            Add("Engine.Description.Ready", "O motor está pronto.", "The engine is ready.");
            Add("Engine.Description.ReadyWithVersion",
                "O motor {0} está pronto e verificado.",
                "Engine {0} is ready and verified.");
            Add("Engine.Description.VersionMismatch",
                "O motor instalado não é a versão que esta aplicação espera.",
                "The installed engine is not the version this application expects.");
            Add("Engine.Description.VersionMismatchDetail",
                "O motor instalado reporta {0} e esta aplicação espera {1}.",
                "The installed engine reports {0} and this application expects {1}.");
            Add("Engine.Description.IntegrityFailure",
                "O motor instalado não corresponde à assinatura registada no empacotamento.",
                "The installed engine does not match the signature recorded at packaging time.");
            Add("Engine.Description.Missing",
                "Não foi encontrado nenhum motor nesta instalação.",
                "No engine was found in this installation.");
            Add("Engine.Description.Unsupported",
                "O motor não respondeu à verificação de versão de forma reconhecível.",
                "The engine did not answer the version check in a recognizable way.");

            // ---- Home -----------------------------------------------------------------------
            Add("Home.EngineStateCaption", "ESTADO DO MOTOR", "ENGINE STATE");
            Add("Home.StartCaption", "COMEÇAR", "GET STARTED");
            Add("Home.RecentCaption", "ÚLTIMOS RESULTADOS", "RECENT RESULTS");
            Add("Home.NoRecent",
                "Ainda não foi gerado nenhum resultado nesta máquina.",
                "No result has been produced on this machine yet.");
            Add("Home.QuickReport", "Relatório rápido", "Quick report");
            Add("Home.CreateSnapshot", "Criar snapshot", "Create snapshot");
            Add("Home.CompareRevisions", "Comparar revisões", "Compare revisions");

            // ---- Common actions -------------------------------------------------------------
            Add("Action.Open", "Abrir", "Open");
            Add("Action.Reveal", "Mostrar na pasta", "Show in folder");
            Add("Action.Cancel", "Cancelar", "Cancel");
            Add("Action.Run", "Executar", "Run");
            Add("Action.Clear", "Limpar", "Clear");
            Add("Action.Add", "Adicionar…", "Add…");
            Add("Action.Remove", "Remover", "Remove");
            Add("Action.MoveUp", "Subir", "Move up");
            Add("Action.MoveDown", "Descer", "Move down");
            Add("Action.PickFile", "Escolher ficheiro…", "Choose file…");
            Add("Action.PickDestination", "Escolher destino…", "Choose destination…");
            Add("Action.Understood", "Compreendi", "Got it");

            Add("Field.NotChosen", "Ainda não escolhido.", "Not chosen yet.");

            // ---- Quick report ---------------------------------------------------------------
            Add("QuickReport.Title", "Relatório rápido", "Quick report");
            Add("QuickReport.Description",
                "Um export XML do Clash Detective, agrupado num relatório HTML autónomo.",
                "One Clash Detective XML export, grouped into a self-contained HTML report.");
            Add("QuickReport.Step1", "1. EXPORT XML", "1. XML EXPORT");
            Add("QuickReport.Step2", "2. DESTINO DO RELATÓRIO", "2. REPORT DESTINATION");
            Add("QuickReport.DropHint",
                "Ou arraste o ficheiro para esta janela.",
                "Or drag the file onto this window.");
            Add("QuickReport.Generate", "Gerar relatório", "Generate report");
            Add("QuickReport.PickInput",
                "Escolher export XML do Clash Detective",
                "Choose a Clash Detective XML export");
            Add("QuickReport.PickOutput", "Guardar relatório como", "Save report as");

            // ---- What the action beside the button is waiting for ---------------------------
            // Readiness is a claim about the form, not about the runner, so it is only ever stated
            // when every value the operation needs is actually present.
            Add("Action.ReadyToRun", "Pronto para executar.", "Ready to run.");
            Add("Action.NotReadyToRun",
                "Faltam dados para executar.",
                "Something is still missing before this can run.");

            // ---- Operation runner -----------------------------------------------------------
            Add("Runner.Idle", "Ainda não foi executado.", "Not run yet.");
            Add("Runner.Running", "A executar o motor…", "Running the engine…");
            Add("Runner.Succeeded", "Concluído.", "Finished.");
            Add("Runner.Failed", "A operação falhou.", "The operation failed.");
            Add("Runner.Canceled", "A operação foi cancelada.", "The operation was cancelled.");
            Add("Runner.ProducedCaption", "FICHEIRO PRODUZIDO", "FILE PRODUCED");
            Add("Runner.EngineDetailCaption", "DETALHE DO MOTOR", "ENGINE DETAIL");
            Add("Runner.EngineOutputCaption", "SAÍDA DO MOTOR", "ENGINE OUTPUT");

            // ---- Errors, resolved from the error code rather than from engine text -----------
            Add("Error.EngineExecutionFailure",
                "O motor terminou com um erro.",
                "The engine ended with an error.");
            Add("Error.OutputMissing",
                "O motor terminou com sucesso mas o ficheiro de saída não foi produzido.",
                "The engine finished successfully but the output file was not produced.");
            Add("Error.EngineMissing",
                "Não foi encontrado nenhum motor nesta instalação.",
                "No engine was found in this installation.");
            Add("Error.EngineIntegrityFailure",
                "O motor instalado não corresponde à assinatura registada no empacotamento.",
                "The installed engine does not match the signature recorded at packaging time.");
            Add("Error.EngineVersionMismatch",
                "O motor instalado não é a versão que esta aplicação espera.",
                "The installed engine is not the version this application expects.");
            Add("Error.EngineUnsupported",
                "O motor não respondeu de forma reconhecível.",
                "The engine did not answer in a recognizable way.");
            Add("Error.Canceled", "A operação foi cancelada.", "The operation was cancelled.");
            Add("Error.TimedOut",
                "O motor não terminou dentro do tempo previsto.",
                "The engine did not finish within the expected time.");
            Add("Error.EngineStartFailure",
                "Não foi possível iniciar o motor.",
                "The engine could not be started.");
            Add("Error.InvalidRequest",
                "A operação não pôde ser iniciada com os valores indicados.",
                "The operation could not be started with the values supplied.");
            Add("Error.LocalIoFailure",
                "Não foi possível ler ou escrever um ficheiro local desta aplicação.",
                "A local file belonging to this application could not be read or written.");
            Add("Error.CollisionCanceled",
                "A operação não foi iniciada porque o ficheiro de destino já existe.",
                "The operation was not started because the destination file already exists.");
            Add("Error.AlreadyRunning",
                "Já está a decorrer outra operação nesta janela.",
                "Another operation is already running in this window.");

            // ---- Warnings -------------------------------------------------------------------
            Add("Warning.DuplicateSnapshotReference",
                "O mesmo snapshot foi declarado mais do que uma vez.",
                "The same snapshot was declared more than once.");
            Add("Warning.EngineWroteToStandardError",
                "O motor terminou com sucesso mas escreveu mensagens de diagnóstico.",
                "The engine finished successfully but wrote diagnostic messages.");
            Add("Warning.OutputTruncated",
                "O motor produziu mais texto do que é possível manter; o meio do registo foi omitido.",
                "The engine produced more text than can be kept; the middle of the log was dropped.");
            Add("Warning.ExperimentalOperation",
                "Esta operação usa comportamento do motor ainda experimental.",
                "This operation exercises engine behaviour that is still experimental.");

            // ---- Forms ----------------------------------------------------------------------
            Add("Form.Experimental",
                "Experimental: a comparação entre revisões ainda não foi validada com três exports "
                    + "históricos reais.",
                "Experimental: comparing revisions has not been validated against three real "
                    + "historical exports.");

            Add("Section.Snapshots.Title", "Snapshots", "Snapshots");
            Add("Section.Snapshots.Description",
                "Um snapshot é evidência imutável de um run. O motor nunca substitui um snapshot existente.",
                "A snapshot is immutable evidence of one run. The engine never replaces an existing snapshot.");
            Add("Section.Longitudinal.Title", "Longitudinal", "Longitudinal");
            Add("Section.Longitudinal.Description",
                "A ordem dos runs é sempre declarada por si. Nada é ordenado por data, nome ou revisão.",
                "Run order is always yours to declare. Nothing is sorted by date, name, or revision.");
            Add("Section.Projects.Title", "Projetos", "Projects");
            Add("Section.Projects.Description",
                "O catálogo guarda referências operacionais. Os snapshots continuam a ser a única evidência.",
                "The catalog holds operational references. Snapshots remain the only evidence.");
            Add("Section.Governance.Title", "Governança", "Governance");
            Add("Section.Governance.Description",
                "A identidade persistente de um clash só existe quando um humano a confirma. "
                    + "Uma sugestão do algoritmo nunca é uma decisão.",
                "A clash's persistent identity exists only when a human confirms it. "
                    + "An algorithmic suggestion is never a decision.");

            Add("Form.CreateSnapshot.Title", "Criar snapshot", "Create snapshot");
            Add("Form.CreateSnapshot.Description",
                "Guarda um run de coordenação como evidência imutável. O motor nunca substitui um "
                    + "snapshot existente.",
                "Stores one coordination run as immutable evidence. The engine never replaces an "
                    + "existing snapshot.");

            Add("Form.CompareSnapshots.Title", "Comparar snapshots", "Compare snapshots");
            Add("Form.CompareRuns.Title", "Comparar duas revisões", "Compare two revisions");
            Add("Form.ExplicitRoles",
                "Anterior e atual são papéis declarados por si. A aplicação nunca deduz a ordem por "
                    + "data ou nome.",
                "Previous and current are roles you declare. The application never infers the order "
                    + "from a date or a name.");

            Add("Form.IndexSnapshots.Title", "Criar índice de runs", "Create run index");
            Add("Form.IndexSnapshots.Description",
                "A ordem que declarar é a ordem do índice. Nada é ordenado por data, nome ou revisão, "
                    + "e um snapshot repetido é mantido tal como o declarou.",
                "The order you declare is the index's order. Nothing is sorted by date, name, or "
                    + "revision, and a repeated snapshot is kept exactly as you declared it.");

            Add("Form.CompareIndex.Title", "Comparar índice de runs", "Compare run index");
            Add("Form.CompareIndex.Description",
                "Compara apenas pares adjacentes, pela ordem declarada no índice.",
                "Compares adjacent pairs only, in the order declared in the index.");

            Add("Form.CreateProject.Title", "Criar projeto", "Create project");
            Add("Form.CreateProject.Description",
                "O catálogo guarda apenas referências operacionais. Os snapshots continuam a ser a "
                    + "única evidência.",
                "The catalog holds operational references only. Snapshots remain the only evidence.");

            Add("Form.AppendProjectSnapshot.Title",
                "Acrescentar snapshot ao projeto", "Append snapshot to project");
            Add("Form.AppendProjectSnapshot.Description",
                "Acrescenta uma referência ao fim do índice. Nada é reordenado, removido ou "
                    + "desduplicado, e o relatório não é regenerado automaticamente.",
                "Appends one reference at the end of the index. Nothing is reordered, removed, or "
                    + "deduplicated, and the report is not regenerated automatically.");
            Add("Form.AppendProjectSnapshot.FollowUp",
                "Renderizar projeto agora", "Render project now");

            Add("Form.RenderProject.Title",
                "Regenerar relatório do projeto", "Regenerate project report");
            Add("Form.RenderProject.Description",
                "O destino vem do catálogo existente. A aplicação não escolhe nem inventa esse destino.",
                "The destination comes from the existing catalog. The application neither chooses "
                    + "nor invents it.");

            Add("Form.CreateGovernance.Title",
                "Criar documento de governança", "Create governance document");
            Add("Form.CreateGovernance.Description",
                "Cria um documento vazio ligado a um projeto. As decisões são acrescentadas uma a uma, depois.",
                "Creates an empty document bound to a project. Decisions are appended one at a time, later.");

            Add("Form.AppendDecision.Title", "Registar decisão humana", "Record a human decision");
            Add("Form.AppendDecision.Description",
                "Uma sugestão do algoritmo nunca é uma decisão. Confiança alta não significa "
                    + "confirmado. A identidade persistente só existe quando um humano a confirma aqui.",
                "An algorithmic suggestion is never a decision. High confidence does not mean "
                    + "confirmed. Persistent identity exists only when a human confirms it here.");

            Add("Form.ValidateGovernance.Title", "Validar governança", "Validate governance");
            Add("Form.ValidateGovernance.Description",
                "Verifica apenas a ligação ao projeto e a existência dos pontos de evidência. Não "
                    + "escreve nada.",
                "Checks only the project binding and the existence of the evidence endpoints. It "
                    + "writes nothing.");

            Add("Form.RenderGovernanceReport.Title",
                "Gerar revisão de governança", "Render governance review");
            Add("Form.RenderGovernanceReport.Description",
                "Apresenta as decisões persistidas pela ordem em que foram registadas. É um "
                    + "artefacto derivado, não evidência, e o motor só o gera depois de a validação passar.",
                "Presents the persisted decisions in the order they were recorded. It is a derived "
                    + "artifact, not evidence, and the engine renders it only after validation passes.");

            // ---- Field labels and hints ------------------------------------------------------
            Add("Field.Xml.Label", "Export XML", "XML export");
            Add("Field.Xml.Hint",
                "O export do Clash Detective deste run.",
                "This run's Clash Detective export.");
            Add("Field.Manifest.Label", "Run manifest", "Run manifest");
            Add("Field.Manifest.Hint",
                "As revisões e os testes executados, declarados explicitamente.",
                "The revisions and executed tests, declared explicitly.");
            Add("Field.Snapshot.Label", "Snapshot", "Snapshot");
            Add("Field.Snapshot.Hint", "Onde guardar o snapshot.", "Where to store the snapshot.");
            Add("Field.PreviousSnapshot.Label", "Snapshot anterior", "Previous snapshot");
            Add("Field.PreviousSnapshot.Hint",
                "O run mais antigo desta comparação.", "The older run in this comparison.");
            Add("Field.CurrentSnapshot.Label", "Snapshot atual", "Current snapshot");
            Add("Field.CurrentSnapshot.Hint",
                "O run mais recente desta comparação.", "The newer run in this comparison.");
            Add("Field.Report.Label", "Relatório", "Report");
            Add("Field.Report.Hint",
                "Onde guardar o relatório da comparação.", "Where to store the comparison report.");
            Add("Field.PreviousXml.Label", "Export XML anterior", "Previous XML export");
            Add("Field.PreviousXml.Hint", "O export do run mais antigo.", "The older run's export.");
            Add("Field.PreviousManifest.Label", "Manifest anterior", "Previous manifest");
            Add("Field.PreviousManifest.Hint", "O manifest do run mais antigo.", "The older run's manifest.");
            Add("Field.CurrentXml.Label", "Export XML atual", "Current XML export");
            Add("Field.CurrentXml.Hint", "O export do run mais recente.", "The newer run's export.");
            Add("Field.CurrentManifest.Label", "Manifest atual", "Current manifest");
            Add("Field.CurrentManifest.Hint", "O manifest do run mais recente.", "The newer run's manifest.");
            Add("Field.OrderedSnapshots.Label",
                "Snapshots, na ordem declarada", "Snapshots, in the order you declare");
            Add("Field.Index.Label", "Índice de runs", "Run index");
            Add("Field.Index.Hint",
                "O índice que declara a ordem dos runs.", "The index that declares the run order.");
            Add("Field.IndexDestination.Label", "Índice", "Index");
            Add("Field.IndexDestination.Hint", "Onde guardar o índice de runs.", "Where to store the run index.");
            Add("Field.ExistingIndex.Hint",
                "O índice já existente deste projeto.", "This project's existing index.");
            Add("Field.LongitudinalReport.Label", "Relatório longitudinal", "Longitudinal report");
            Add("Field.LongitudinalReport.Hint",
                "Onde guardar o relatório longitudinal.", "Where to store the longitudinal report.");
            Add("Field.ProjectId.Label", "Identificador do projeto", "Project id");
            Add("Field.ProjectId.Hint",
                "Estável e usado também na governança de identidade.",
                "Stable, and used by identity governance too.");
            Add("Field.ProjectId.GovernanceHint",
                "Tem de ser exatamente o mesmo do catálogo do projeto.",
                "Must be exactly the same as the project catalog's.");
            Add("Field.DisplayName.Label", "Nome", "Name");
            Add("Field.DisplayName.Hint",
                "O nome que aparece no relatório.", "The name shown in the report.");
            Add("Field.ReportDestination.Label",
                "Destino do relatório longitudinal", "Longitudinal report destination");
            Add("Field.ReportDestination.Hint",
                "Onde o relatório será regenerado. A pasta tem de existir.",
                "Where the report will be regenerated. The folder must already exist.");
            Add("Field.ProjectFile.Label", "Ficheiro do projeto", "Project file");
            Add("Field.ProjectFile.Hint",
                "Onde guardar o catálogo do projeto.", "Where to store the project catalog.");
            Add("Field.Project.Label", "Projeto", "Project");
            Add("Field.Project.Hint", "O catálogo do projeto.", "The project catalog.");
            Add("Field.AppendedSnapshot.Label", "Snapshot a acrescentar", "Snapshot to append");
            Add("Field.AppendedSnapshot.Hint",
                "O run a juntar ao fim do índice.", "The run to add at the end of the index.");
            Add("Field.Governance.Label", "Documento de governança", "Governance document");
            Add("Field.Governance.DestinationHint", "Onde guardar o documento.", "Where to store the document.");
            Add("Field.Governance.AppendHint",
                "O documento onde a decisão é acrescentada.", "The document the decision is appended to.");
            Add("Field.Governance.ValidateHint", "O documento a validar.", "The document to validate.");
            Add("Field.Governance.RenderHint", "O documento a apresentar.", "The document to present.");
            Add("Field.ReviewReport.Label", "Relatório de revisão", "Review report");
            Add("Field.ReviewReport.Hint", "Onde guardar a revisão.", "Where to store the review.");
            Add("Field.DecisionId.Label", "Identificador da decisão", "Decision id");
            Add("Field.DecisionId.Hint",
                "Um identificador estável desta decisão.", "A stable identifier for this decision.");
            Add("Field.LeftRunId.Label", "Run à esquerda", "Left run");
            Add("Field.LeftRunId.Hint", "O run id do lado esquerdo.", "The left side's run id.");
            Add("Field.RightRunId.Label", "Run à direita", "Right run");
            Add("Field.RightRunId.Hint", "O run id do lado direito.", "The right side's run id.");
            Add("Field.LeftOccurrence.Label", "Ocorrência à esquerda", "Left occurrence");
            Add("Field.RightOccurrence.Label", "Ocorrência à direita", "Right occurrence");
            Add("Field.Occurrence.Hint",
                "Índice da ocorrência dentro do snapshot, a começar em 0. A existência é verificada "
                    + "pelo motor.",
                "The occurrence's index within the snapshot, starting at 0. Existence is checked by "
                    + "the engine.");
            Add("Field.Occurrence.FormatError",
                "Indique um número inteiro maior ou igual a 0.",
                "Enter a whole number greater than or equal to 0.");
            Add("Field.ReviewerAlias.Label", "Alias do revisor", "Reviewer alias");
            Add("Field.ReviewerAlias.Hint",
                "Um alias. Não é preciso nome real, email ou login.",
                "An alias. No real name, email, or login is needed.");
            Add("Field.PersistentIdentityId.Label", "Identificador persistente", "Persistent identity id");
            Add("Field.PersistentIdentityId.Hint",
                "Obrigatório numa confirmação. Nunca usado numa rejeição.",
                "Required for a confirmation. Never used for a rejection.");
            Add("Field.Reason.Label", "Motivo", "Reason");
            Add("Field.Reason.Hint", "Opcional.", "Optional.");

            Add("OrderedList.DuplicatesWarning",
                "Há snapshots repetidos. São mantidos exatamente como os declarou; nada é removido.",
                "There are repeated snapshots. They are kept exactly as you declared them; nothing "
                    + "is removed.");
            Add("OrderedList.OrderNote",
                "A ordem desta lista é a ordem do índice. Nada é ordenado automaticamente.",
                "This list's order is the index's order. Nothing is sorted automatically.");
            Add("OrderedList.DuplicateBadge", "repetido", "repeated");

            // ---- Identity decisions ----------------------------------------------------------
            Add("Decision.Label", "Decisão", "Decision");
            Add("Decision.Confirm.Label", "Confirmar mesma identidade", "Confirm same identity");
            Add("Decision.Confirm.Explanation",
                "Declara que as duas ocorrências são o mesmo clash persistente. Exige um "
                    + "identificador persistente.",
                "Declares that the two occurrences are the same persistent clash. Requires a "
                    + "persistent identity id.");
            Add("Decision.Reject.Label", "Rejeitar mesma identidade", "Reject same identity");
            Add("Decision.Reject.Explanation",
                "Declara que as duas ocorrências não são o mesmo clash. Nunca leva identificador "
                    + "persistente.",
                "Declares that the two occurrences are not the same clash. Never carries a "
                    + "persistent identity id.");

            // ---- Output collision -------------------------------------------------------------
            Add("Collision.Title", "O ficheiro já existe", "The file already exists");
            Add("Collision.Heading",
                "Já existe um relatório com este nome", "A report with this name already exists");
            Add("Collision.Explanation",
                "Substituir apaga o relatório anterior. Escolher outro nome mantém os dois.",
                "Replacing deletes the previous report. Choosing another name keeps both.");
            Add("Collision.ChooseAnother", "Escolher outro nome", "Choose another name");
            Add("Collision.Replace", "Substituir relatório existente", "Replace the existing report");

            // ---- Interrupted jobs --------------------------------------------------------------
            Add("Interrupted.Title", "Operação interrompida", "Operation interrupted");
            Add("Interrupted.Explanation",
                "A aplicação fechou enquanto isto estava a correr. Nada é retomado automaticamente: "
                    + "verifique o ficheiro de destino antes de repetir a operação.",
                "The application closed while this was running. Nothing is resumed automatically: "
                    + "check the destination file before repeating the operation.");

            // ---- Operation names, used when reporting an interrupted job -----------------------
            Add("Operation.QuickReport", "Relatório rápido", "Quick report");
            Add("Operation.CreateSnapshot", "Criar snapshot", "Create snapshot");
            Add("Operation.CompareRuns", "Comparar duas revisões", "Compare two revisions");
            Add("Operation.CompareSnapshots", "Comparar snapshots", "Compare snapshots");
            Add("Operation.IndexSnapshots", "Criar índice de runs", "Create run index");
            Add("Operation.CompareIndex", "Comparar índice de runs", "Compare run index");
            Add("Operation.CreateProject", "Criar projeto", "Create project");
            Add("Operation.AppendProjectSnapshot",
                "Acrescentar snapshot ao projeto", "Append snapshot to project");
            Add("Operation.RenderProject",
                "Regenerar relatório do projeto", "Regenerate project report");
            Add("Operation.CreateIdentityGovernance",
                "Criar documento de governança", "Create governance document");
            Add("Operation.AppendIdentityDecision",
                "Registar decisão humana", "Record a human decision");
            Add("Operation.ValidateIdentityGovernance", "Validar governança", "Validate governance");
            Add("Operation.RenderIdentityGovernanceReport",
                "Gerar revisão de governança", "Render governance review");

            // ---- File picker titles ------------------------------------------------------------
            Add("Picker.ClashXml",
                "Export XML do Clash Detective", "Clash Detective XML export");
            Add("Picker.HtmlReport", "Relatório HTML", "HTML report");
            Add("Picker.RunManifestJson", "Run manifest JSON", "Run manifest JSON");
            Add("Picker.SnapshotJson", "Snapshot JSON", "Snapshot JSON");
            Add("Picker.RunIndexJson", "Run index JSON", "Run index JSON");
            Add("Picker.ProjectCatalogJson", "Projeto JSON", "Project JSON");
            Add("Picker.IdentityGovernanceJson", "Governança JSON", "Governance JSON");

            // ---- Settings ------------------------------------------------------------------------
            Add("Settings.Title", "Definições", "Settings");
            Add("Settings.AppearanceCaption", "APARÊNCIA", "APPEARANCE");
            Add("Settings.Theme", "Tema", "Theme");
            Add("Settings.Theme.System", "Seguir o sistema", "Follow the system");
            Add("Settings.Theme.Light", "Claro", "Light");
            Add("Settings.Theme.Dark", "Escuro", "Dark");
            Add("Settings.Language", "Idioma", "Language");
            Add("Settings.Language.System", "Seguir o sistema", "Follow the system");
            Add("Settings.Language.Portuguese", "Português", "Portuguese");
            Add("Settings.Language.English", "Inglês", "English");
            Add("Settings.WarningsCaption", "AVISOS", "WARNINGS");
            Add("Settings.ShowExperimental",
                "Mostrar aviso quando uma operação for experimental",
                "Warn when an operation is experimental");
            Add("Settings.ExperimentalNote",
                "A comparação entre revisões ainda não foi validada com três exports históricos reais.",
                "Comparing revisions has not been validated against three real historical exports.");
            Add("Settings.LocalDataCaption", "DADOS LOCAIS", "LOCAL DATA");
            Add("Settings.LocalDataIntro",
                "Esta aplicação guarda definições, registos e estado de trabalhos em:",
                "This application keeps its settings, logs, and job state in:");
            Add("Settings.LocalDataNote",
                "Os seus projetos, snapshots e relatórios nunca são guardados aqui e não são "
                    + "removidos ao desinstalar.",
                "Your projects, snapshots, and reports are never stored here and are not removed "
                    + "when you uninstall.");
            Add("Settings.Version", "Versão da aplicação", "Application version");
            Add("Settings.PrivacyNote",
                "Sem telemetria, sem envio de dados e sem conta. Tudo permanece nesta máquina.",
                "No telemetry, no uploads, and no account. Everything stays on this machine.");

            // ---- Diagnostics -----------------------------------------------------------------------
            Add("Diagnostics.Caption", "DIAGNÓSTICO", "DIAGNOSTICS");
            Add("Diagnostics.Explanation",
                "Um pacote de diagnóstico contém apenas os ficheiros listados abaixo. Nunca inclui "
                    + "ficheiros do cliente, caminhos absolutos, argumentos de execução ou o conteúdo "
                    + "dos seus modelos.",
                "A diagnostic bundle contains only the files listed below. It never includes client "
                    + "files, absolute paths, execution arguments, or the contents of your models.");
            Add("Diagnostics.ShowPreview", "Ver o que seria incluído", "See what would be included");
            Add("Diagnostics.FilesCaption", "FICHEIROS INCLUÍDOS", "FILES INCLUDED");
            Add("Diagnostics.LogCaption",
                "PRÉ-VISUALIZAÇÃO DO REGISTO REDIGIDO", "REDACTED LOG PREVIEW");
            Add("Diagnostics.NoLog",
                "Ainda não há registos nesta máquina.", "There are no logs on this machine yet.");
            Add("Diagnostics.Create", "Gerar pacote de diagnóstico", "Create diagnostic bundle");
            Add("Diagnostics.Save", "Guardar pacote de diagnóstico", "Save diagnostic bundle");
            Add("Diagnostics.WriteFailed",
                "Não foi possível escrever o pacote de diagnóstico: {0}",
                "The diagnostic bundle could not be written: {0}");

            // ---- Placeholder ------------------------------------------------------------------------
            Add("Placeholder.NotWired",
                "Esta secção ainda não está ligada ao motor nesta versão da aplicação.",
                "This section is not wired to the engine in this version of the application yet.");

            return new ReadOnlyDictionary<string, UiText>(text);
        }
    }
}
