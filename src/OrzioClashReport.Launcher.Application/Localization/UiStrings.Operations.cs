using System.Collections.Generic;

namespace OrzioClashReport.Launcher.Application.Localization
{
    internal static partial class UiStrings
    {
        /// <summary>
        /// The eleven operation forms: title, description, field labels and descriptions, and notes.
        /// Keys are <c>op.&lt;form&gt;.title</c>, <c>.description</c>, <c>.&lt;field&gt;.label</c>,
        /// <c>.&lt;field&gt;.description</c> and <c>.note.&lt;n&gt;</c>.
        /// </summary>
        private static void AddOperationForms(Dictionary<string, (string Portuguese, string English)> t)
        {
            // Snapshots.
            Add(t, "op.snapshot.title", "Criar snapshot", "Create snapshot");
            Add(t, "op.snapshot.description",
                "Congela um run como evidência imutável. O snapshot nunca é substituído: "
                + "se o destino já existir, escolha outro nome.",
                "Freezes a run as immutable evidence. The snapshot is never replaced: "
                + "if the destination already exists, choose another name.");
            Add(t, "op.snapshot.xml.label", "Export XML", "XML export");
            Add(t, "op.snapshot.xml.description",
                "O ficheiro exportado pelo Clash Detective.",
                "The file exported by Clash Detective.");
            Add(t, "op.snapshot.manifest.label", "Run manifest", "Run manifest");
            Add(t, "op.snapshot.manifest.description",
                "As revisões e os testes executados declarados para este run.",
                "The revisions and executed tests declared for this run.");
            Add(t, "op.snapshot.output.label", "Destino do snapshot", "Snapshot destination");
            Add(t, "op.snapshot.output.description",
                "Onde o snapshot JSON será criado.",
                "Where the snapshot JSON will be created.");
            Add(t, "op.snapshot.note.1",
                "O snapshot é evidência: é sempre criado de novo e nunca sobrescrito.",
                "The snapshot is evidence: it is always created anew and never overwritten.");

            Add(t, "op.compareSnapshots.title", "Comparar dois snapshots", "Compare two snapshots");
            Add(t, "op.compareSnapshots.description",
                "Compara dois snapshots já criados. Os papéis anterior e atual são seus: "
                + "nada é ordenado por data, revisão ou nome de ficheiro.",
                "Compares two snapshots that already exist. The previous and current roles are yours: "
                + "nothing is ordered by date, revision or file name.");
            Add(t, "op.compareSnapshots.previous.label", "Snapshot anterior", "Previous snapshot");
            Add(t, "op.compareSnapshots.previous.description",
                "O run que serve de ponto de partida.",
                "The run that serves as the starting point.");
            Add(t, "op.compareSnapshots.current.label", "Snapshot atual", "Current snapshot");
            Add(t, "op.compareSnapshots.current.description",
                "O run que serve de ponto de chegada.",
                "The run that serves as the end point.");
            Add(t, "op.compareSnapshots.output.label", "Destino do relatório", "Report destination");
            Add(t, "op.compareSnapshots.output.description",
                "O HTML da comparação. É derivado e pode ser gerado de novo.",
                "The comparison HTML. It is derived and can be generated again.");
            Add(t, "op.compareSnapshots.note.1",
                "Matching e lifecycle são recalculados a partir da evidência, e não são guardados.",
                "Matching and lifecycle are recalculated from the evidence and are not stored.");
            Add(t, "op.compareSnapshots.note.2",
                "Comparação longitudinal continua experimental: ainda não foi validada em exports reais sequenciais.",
                "Longitudinal comparison remains experimental: it has not yet been validated "
                + "on sequential real exports.");

            // Longitudinal.
            Add(t, "op.indexSnapshots.title", "Criar índice ordenado", "Create ordered index");
            Add(t, "op.indexSnapshots.description",
                "Declara explicitamente a ordem dos runs. Esta ordem é a única autoridade de sequência.",
                "Explicitly declares the order of the runs. This order is the only authority on sequence.");
            Add(t, "op.indexSnapshots.snapshots.label", "Snapshots, por ordem", "Snapshots, in order");
            Add(t, "op.indexSnapshots.snapshots.description",
                "Use as setas para definir a sequência. Nada é ordenado automaticamente por data, nome ou revisão.",
                "Use the arrows to set the sequence. Nothing is ordered automatically "
                + "by date, name or revision.");
            Add(t, "op.indexSnapshots.output.label", "Destino do índice", "Index destination");
            Add(t, "op.indexSnapshots.output.description",
                "Onde o run-index JSON será criado. O índice é sempre criado de novo.",
                "Where the run-index JSON will be created. The index is always created anew.");
            Add(t, "op.indexSnapshots.note.1",
                "Um snapshot repetido é mantido tal como o declarou; nunca é removido em silêncio.",
                "A repeated snapshot is kept as you declared it; it is never removed silently.");

            Add(t, "op.compareIndex.title", "Comparar índice", "Compare index");
            Add(t, "op.compareIndex.description",
                "Percorre apenas pares adjacentes, na ordem declarada no índice.",
                "Walks adjacent pairs only, in the order declared in the index.");
            Add(t, "op.compareIndex.index.label", "Índice de runs", "Run index");
            Add(t, "op.compareIndex.index.description",
                "O run-index JSON que declara a sequência.",
                "The run-index JSON that declares the sequence.");
            Add(t, "op.compareIndex.output.label", "Destino do relatório", "Report destination");
            Add(t, "op.compareIndex.output.description",
                "O HTML longitudinal. É derivado e pode ser gerado de novo.",
                "The longitudinal HTML. It is derived and can be generated again.");
            Add(t, "op.compareIndex.note.1",
                "Os caminhos de continuidade são derivados e recalculáveis: não provam que se trata do mesmo clash.",
                "Continuity paths are derived and recalculable: they do not prove it is the same clash.");
            Add(t, "op.compareIndex.note.2",
                "Comparação longitudinal continua experimental: ainda não foi validada em três exports reais sequenciais.",
                "Longitudinal comparison remains experimental: it has not yet been validated "
                + "on three sequential real exports.");

            Add(t, "op.compare.title", "Comparar dois runs", "Compare two runs");
            Add(t, "op.compare.description",
                "Compara dois exports com os respetivos manifests. "
                + "Anterior e atual são papéis explícitos que você declara.",
                "Compares two exports with their respective manifests. "
                + "Previous and current are explicit roles that you declare.");
            Add(t, "op.compare.previousXml.label", "XML anterior", "Previous XML");
            Add(t, "op.compare.previousXml.description",
                "O export do run de partida.",
                "The export of the starting run.");
            Add(t, "op.compare.previousManifest.label", "Manifest anterior", "Previous manifest");
            Add(t, "op.compare.previousManifest.description",
                "As revisões declaradas para o run de partida.",
                "The revisions declared for the starting run.");
            Add(t, "op.compare.currentXml.label", "XML atual", "Current XML");
            Add(t, "op.compare.currentXml.description",
                "O export do run de chegada.",
                "The export of the end run.");
            Add(t, "op.compare.currentManifest.label", "Manifest atual", "Current manifest");
            Add(t, "op.compare.currentManifest.description",
                "As revisões declaradas para o run de chegada.",
                "The revisions declared for the end run.");
            Add(t, "op.compare.output.label", "Destino do relatório", "Report destination");
            Add(t, "op.compare.output.description", "O HTML da comparação.", "The comparison HTML.");
            Add(t, "op.compare.note.1",
                "A ordem cronológica nunca é inferida: os papéis anterior e atual são os que declarar aqui.",
                "Chronological order is never inferred: the previous and current roles "
                + "are the ones you declare here.");
            Add(t, "op.compare.note.2",
                "Comparação longitudinal continua experimental: ainda não foi validada em exports reais sequenciais.",
                "Longitudinal comparison remains experimental: it has not yet been validated "
                + "on sequential real exports.");

            // Projects.
            Add(t, "op.createProject.title", "Criar projeto", "Create project");
            Add(t, "op.createProject.description",
                "Guarda as referências operacionais do projeto: o índice de runs e o destino do relatório longitudinal.",
                "Stores the project's operational references: the run index and "
                + "the longitudinal report destination.");
            Add(t, "op.createProject.projectId.label", "Identificador do projeto", "Project identifier");
            Add(t, "op.createProject.projectId.description",
                "Usado para ligar decisões de governança a este projeto. Comparado exatamente, sem normalização.",
                "Used to link governance decisions to this project. Compared exactly, without normalization.");
            Add(t, "op.createProject.name.label", "Nome do projeto", "Project name");
            Add(t, "op.createProject.name.description",
                "O nome legível mostrado nos relatórios.",
                "The readable name shown in reports.");
            Add(t, "op.createProject.index.label", "Índice de runs", "Run index");
            Add(t, "op.createProject.index.description",
                "O run-index JSON já criado.",
                "The run-index JSON already created.");
            Add(t, "op.createProject.report.label",
                "Destino do relatório longitudinal",
                "Longitudinal report destination");
            Add(t, "op.createProject.report.description",
                "Onde o HTML do projeto será escrito. O ficheiro ainda não precisa de existir, mas a pasta sim.",
                "Where the project's HTML will be written. The file does not need to exist yet, "
                + "but the folder must.");
            Add(t, "op.createProject.output.label", "Destino do catálogo", "Catalog destination");
            Add(t, "op.createProject.output.description",
                "Onde o project.json será criado. O catálogo é sempre criado de novo.",
                "Where project.json will be created. The catalog is always created anew.");
            Add(t, "op.createProject.note.1",
                "O índice, os snapshots e o relatório têm de ficar dentro da árvore de pastas do catálogo.",
                "The index, the snapshots and the report must stay inside the catalog's folder tree.");

            Add(t, "op.appendSnapshot.title", "Acrescentar run ao projeto", "Append run to project");
            Add(t, "op.appendSnapshot.description",
                "Acrescenta um snapshot no fim do índice do projeto. Nada é reordenado nem removido.",
                "Appends a snapshot to the end of the project's index. Nothing is reordered or removed.");
            Add(t, "op.appendSnapshot.project.label", "Catálogo do projeto", "Project catalog");
            Add(t, "op.appendSnapshot.project.description",
                "O project.json do projeto a atualizar.",
                "The project.json of the project to update.");
            Add(t, "op.appendSnapshot.snapshot.label", "Snapshot a acrescentar", "Snapshot to append");
            Add(t, "op.appendSnapshot.snapshot.description",
                "O run que passa a ser o último da sequência.",
                "The run that becomes the last one in the sequence.");
            Add(t, "op.appendSnapshot.note.1",
                "Esta operação altera apenas o índice. O relatório não é regenerado automaticamente.",
                "This operation changes only the index. The report is not regenerated automatically.");
            Add(t, "op.appendSnapshot.followUp",
                "O índice foi atualizado. O relatório longitudinal continua a mostrar a versão anterior "
                + "até o regenerar.",
                "The index was updated. The longitudinal report still shows the previous version "
                + "until you regenerate it.");

            Add(t, "op.renderProject.title", "Renderizar projeto", "Render project");
            Add(t, "op.renderProject.description",
                "Recalcula tudo a partir dos snapshots e reescreve o relatório longitudinal do projeto.",
                "Recalculates everything from the snapshots and rewrites the project's longitudinal report.");
            Add(t, "op.renderProject.project.label", "Catálogo do projeto", "Project catalog");
            Add(t, "op.renderProject.project.description",
                "O project.json cujo relatório será regenerado.",
                "The project.json whose report will be regenerated.");
            Add(t, "op.renderProject.note.1",
                "O destino do relatório vem do catálogo do projeto, em leitura apenas.",
                "The report destination comes from the project catalog, read-only.");
            Add(t, "op.renderProject.note.2",
                "Os snapshots e o índice não são alterados.",
                "Snapshots and the index are not changed.");

            // Governance.
            Add(t, "op.createGovernance.title", "Criar documento de governança", "Create governance document");
            Add(t, "op.createGovernance.description",
                "Cria um documento vazio, ligado a um projeto, onde as suas decisões passam a ser registadas.",
                "Creates an empty document, linked to a project, where your decisions will be recorded.");
            Add(t, "op.createGovernance.projectId.label", "Identificador do projeto", "Project identifier");
            Add(t, "op.createGovernance.projectId.description",
                "Tem de coincidir exatamente com o do catálogo do projeto. Não é normalizado.",
                "Must match the project catalog's exactly. It is not normalized.");
            Add(t, "op.createGovernance.output.label", "Destino do documento", "Document destination");
            Add(t, "op.createGovernance.output.description",
                "Onde o identity-governance JSON será criado. É sempre criado de novo.",
                "Where the identity-governance JSON will be created. It is always created anew.");
            Add(t, "op.createGovernance.note.1",
                "Um documento vazio significa apenas ausência de decisões. Não existe estado pendente.",
                "An empty document only means no decisions. There is no pending state.");

            Add(t, "op.validateGovernance.title", "Validar governança", "Validate governance");
            Add(t, "op.validateGovernance.description",
                "Confirma que cada decisão aponta para um run indexado e para uma ocorrência que existe.",
                "Confirms that each decision points to an indexed run and to an occurrence that exists.");
            Add(t, "op.validateGovernance.project.label", "Catálogo do projeto", "Project catalog");
            Add(t, "op.validateGovernance.project.description",
                "O project.json a que a governança está ligada.",
                "The project.json the governance is linked to.");
            Add(t, "op.validateGovernance.governance.label", "Documento de governança", "Governance document");
            Add(t, "op.validateGovernance.governance.description",
                "O identity-governance.json a validar.",
                "The identity-governance.json to validate.");
            Add(t, "op.validateGovernance.note.1",
                "Esta operação não escreve nada. Valida apenas a ligação ao projeto e a existência das evidências.",
                "This operation writes nothing. It only validates the link to the project "
                + "and the existence of the evidence.");
            Add(t, "op.validateGovernance.note.2",
                "Não valida candidatura do matcher, adjacência, transitividade nem responsabilidade.",
                "It does not validate matcher candidacy, adjacency, transitivity or responsibility.");

            Add(t, "op.renderGovernance.title", "Gerar revisão de governança", "Generate governance review");
            Add(t, "op.renderGovernance.description",
                "Apresenta as decisões registadas, pela ordem em que foram registadas, "
                + "com o lado esquerdo antes do direito.",
                "Presents the recorded decisions, in the order they were recorded, "
                + "with the left side before the right.");
            Add(t, "op.renderGovernance.project.label", "Catálogo do projeto", "Project catalog");
            Add(t, "op.renderGovernance.project.description",
                "O project.json a que a governança está ligada.",
                "The project.json the governance is linked to.");
            Add(t, "op.renderGovernance.governance.label", "Documento de governança", "Governance document");
            Add(t, "op.renderGovernance.governance.description",
                "O identity-governance.json a apresentar.",
                "The identity-governance.json to present.");
            Add(t, "op.renderGovernance.output.label", "Destino da revisão", "Review destination");
            Add(t, "op.renderGovernance.output.description",
                "O HTML da revisão. É derivado e pode ser gerado de novo.",
                "The review HTML. It is derived and can be generated again.");
            Add(t, "op.renderGovernance.note.1",
                "A revisão só é gerada depois de a validação de evidências passar.",
                "The review is only generated after the evidence validation passes.");
            Add(t, "op.renderGovernance.note.2",
                "É um artefacto derivado: não é evidência, não infere identidade e não altera o projeto.",
                "It is a derived artifact: it is not evidence, does not infer identity "
                + "and does not change the project.");

            Add(t, "op.appendDecision.title", "Registar decisão", "Record decision");
            Add(t, "op.appendDecision.description",
                "Regista uma decisão humana explícita sobre duas ocorrências. "
                + "Nenhuma sugestão do algoritmo se torna decisão sozinha.",
                "Records an explicit human decision about two occurrences. "
                + "No algorithm suggestion becomes a decision on its own.");
            Add(t, "op.appendDecision.governance.label", "Documento de governança", "Governance document");
            Add(t, "op.appendDecision.governance.description",
                "O identity-governance.json onde a decisão será acrescentada.",
                "The identity-governance.json the decision will be appended to.");
            Add(t, "op.appendDecision.decisionId.label", "Identificador da decisão", "Decision identifier");
            Add(t, "op.appendDecision.decisionId.description",
                "O seu identificador para esta decisão.",
                "Your identifier for this decision.");
            Add(t, "op.appendDecision.kind.label", "Decisão", "Decision");
            Add(t, "op.appendDecision.kind.description",
                "Confirmar regista que são o mesmo clash. Rejeitar regista que não são.",
                "Confirm records that they are the same clash. Reject records that they are not.");
            Add(t, "op.appendDecision.leftRun.label", "Run id (esquerda)", "Run id (left)");
            Add(t, "op.appendDecision.leftRun.description",
                "O run da primeira ocorrência.",
                "The run of the first occurrence.");
            Add(t, "op.appendDecision.leftIndex.label",
                "Índice da ocorrência (esquerda)",
                "Occurrence index (left)");
            Add(t, "op.appendDecision.rightRun.label", "Run id (direita)", "Run id (right)");
            Add(t, "op.appendDecision.rightRun.description",
                "O run da segunda ocorrência.",
                "The run of the second occurrence.");
            Add(t, "op.appendDecision.rightIndex.label",
                "Índice da ocorrência (direita)",
                "Occurrence index (right)");
            Add(t, "op.appendDecision.occurrenceIndex.description",
                "Zero-based: a primeira ocorrência do run é 0.",
                "Zero-based: the first occurrence of the run is 0.");
            Add(t, "op.appendDecision.alias.label", "Alias do revisor", "Reviewer alias");
            Add(t, "op.appendDecision.alias.description",
                "Um alias basta. Não é pedido nome real, email nem login.",
                "An alias is enough. No real name, email or login is requested.");
            Add(t, "op.appendDecision.alias.example", "coordenador-a", "coordinator-a");
            Add(t, "op.appendDecision.persistentId.label",
                "Identificador persistente",
                "Persistent identifier");
            Add(t, "op.appendDecision.persistentId.description",
                "Obrigatório apenas numa confirmação. Uma rejeição nunca o transporta.",
                "Required only for a confirmation. A rejection never carries it.");
            Add(t, "op.appendDecision.reason.label", "Motivo", "Reason");
            Add(t, "op.appendDecision.reason.description",
                "Opcional. Fica registado tal como o escrever.",
                "Optional. It is recorded exactly as you write it.");
            Add(t, "op.appendDecision.note.1",
                "Uma confiança 'High' do algoritmo não é uma confirmação humana. A decisão é sempre sua.",
                "A 'High' confidence from the algorithm is not a human confirmation. "
                + "The decision is always yours.");
            Add(t, "op.appendDecision.note.2",
                "A existência dos runs e das ocorrências é verificada pelo motor, não por esta janela.",
                "The existence of the runs and occurrences is checked by the engine, not by this window.");
            Add(t, "op.appendDecision.validation.occurrenceIndex",
                "Tem de ser um inteiro maior ou igual a zero.",
                "Must be an integer greater than or equal to zero.");
            Add(t, "op.appendDecision.error.chooseKind",
                "Escolha confirmar ou rejeitar antes de registar a decisão.",
                "Choose confirm or reject before recording the decision.");
            Add(t, "op.appendDecision.error.leftIndex",
                "O índice de ocorrência da esquerda tem de ser um inteiro maior ou igual a zero.",
                "The left occurrence index must be an integer greater than or equal to zero.");
            Add(t, "op.appendDecision.error.rightIndex",
                "O índice de ocorrência da direita tem de ser um inteiro maior ou igual a zero.",
                "The right occurrence index must be an integer greater than or equal to zero.");
        }
    }
}
