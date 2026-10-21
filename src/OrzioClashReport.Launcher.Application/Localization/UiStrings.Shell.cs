using System.Collections.Generic;

namespace OrzioClashReport.Launcher.Application.Localization
{
    internal static partial class UiStrings
    {
        /// <summary>Navigation sections, the engine badge, identity decisions, and the window shell.</summary>
        private static void AddNavigationAndShell(Dictionary<string, (string Portuguese, string English)> t)
        {
            // Navigation sections: label and one-line description, in navigation order.
            Add(t, "nav.home.label", "Início", "Home");
            Add(t, "nav.home.description",
                "Ações rápidas, estado do motor e últimos relatórios.",
                "Quick actions, engine status and latest reports.");

            Add(t, "nav.quickreport.label", "Relatório rápido", "Quick report");
            Add(t, "nav.quickreport.description",
                "Um export XML do Clash Detective para um relatório HTML agrupado.",
                "One Clash Detective XML export to one grouped HTML report.");

            Add(t, "nav.snapshots.label", "Snapshots", "Snapshots");
            Add(t, "nav.snapshots.description",
                "Criar snapshots imutáveis e comparar dois snapshots.",
                "Create immutable snapshots and compare two snapshots.");

            Add(t, "nav.longitudinal.label", "Longitudinal", "Longitudinal");
            Add(t, "nav.longitudinal.description",
                "Índice ordenado de runs e comparação de pares adjacentes.",
                "Ordered run index and comparison of adjacent pairs.");

            Add(t, "nav.projects.label", "Projetos", "Projects");
            Add(t, "nav.projects.description",
                "Catálogo operacional do projeto: criar, acrescentar run e re-renderizar.",
                "Operational project catalog: create, append a run and re-render.");

            Add(t, "nav.governance.label", "Governança", "Governance");
            Add(t, "nav.governance.description",
                "Decisões humanas explícitas de identidade, validação e revisão.",
                "Explicit human identity decisions, validation and review.");

            Add(t, "nav.settings.label", "Definições", "Settings");
            Add(t, "nav.settings.description",
                "Idioma, tema, dados locais, motor e diagnóstico.",
                "Language, theme, local data, engine and diagnostics.");

            // Engine badge: a distinct label per state, so it is never conveyed by colour alone.
            Add(t, "engine.checking.label", "A verificar", "Checking");
            Add(t, "engine.checking.explanation",
                "A confirmar a versão e a integridade do motor.",
                "Confirming the engine's version and integrity.");

            Add(t, "engine.ready.label", "Motor pronto", "Engine ready");
            Add(t, "engine.ready.explanation",
                "O motor respondeu com a versão esperada e passou a verificação de integridade.",
                "The engine answered with the expected version and passed the integrity check.");

            Add(t, "engine.versionMismatch.label", "Versão diferente", "Different version");
            Add(t, "engine.versionMismatch.explanation",
                "O motor instalado não é a versão com que esta aplicação foi empacotada. "
                + "Reinstale para voltar a um par verificado.",
                "The installed engine is not the version this application was packaged with. "
                + "Reinstall to return to a verified pair.");

            Add(t, "engine.integrityFailure.label", "Integridade falhou", "Integrity check failed");
            Add(t, "engine.integrityFailure.explanation",
                "O executável do motor não corresponde ao SHA-256 registado na instalação. "
                + "Reinstale a aplicação.",
                "The engine executable does not match the SHA-256 recorded at installation. "
                + "Reinstall the application.");

            Add(t, "engine.missing.label", "Motor não encontrado", "Engine not found");
            Add(t, "engine.missing.explanation",
                "Não existe motor na pasta de instalação. Reinstale a aplicação.",
                "There is no engine in the installation folder. Reinstall the application.");

            Add(t, "engine.unsupported.label", "Motor não suportado", "Engine not supported");
            Add(t, "engine.unsupported.explanation",
                "O motor não respondeu no formato publicado. Este sistema pode não conseguir executá-lo.",
                "The engine did not answer in the published format. This system may not be able to run it.");

            // Identity decisions. The canonical values (ConfirmSameIdentity / RejectSameIdentity) are
            // never translated and are not in this table.
            Add(t, "decision.confirm.label", "Confirmar mesma identidade", "Confirm same identity");
            Add(t, "decision.confirm.description",
                "Regista que estas duas ocorrências são, para si, o mesmo clash. Exige um identificador persistente.",
                "Records that, in your judgement, these two occurrences are the same clash. "
                + "Requires a persistent identifier.");

            Add(t, "decision.reject.label", "Rejeitar mesma identidade", "Reject same identity");
            Add(t, "decision.reject.description",
                "Regista que estas duas ocorrências não são o mesmo clash. Nunca transporta identificador persistente.",
                "Records that these two occurrences are not the same clash. "
                + "It never carries a persistent identifier.");

            // Window shell: sidebar, status bar, interrupted-operation notice.
            Add(t, "shell.brand.subtitle", "Coordenação BIM", "BIM coordination");
            Add(t, "shell.status.checkingEngine", "A verificar o motor.", "Checking the engine.");

            Add(t, "shell.sidebar.updateAvailable", "Atualização disponível", "Update available");
            Add(t, "shell.sidebar.updateTooltip",
                "Nova versão {0}. Ver detalhes em Definições.",
                "New version {0}. See details in Settings.");
            Add(t, "shell.sidebar.checkTooltip",
                "Procurar atualizações no GitHub",
                "Check for updates on GitHub");

            Add(t, "shell.interrupted.title", "Operação interrompida", "Interrupted operation");
            Add(t, "shell.interrupted.body",
                "A aplicação fechou enquanto isto estava em curso. Nada foi retomado automaticamente. "
                + "Verifique o destino antes de repetir.",
                "The application closed while this was in progress. Nothing was resumed automatically. "
                + "Check the destination before repeating.");
            Add(t, "shell.interrupted.acknowledge", "Compreendido", "Understood");
            Add(t, "shell.interrupted.entry", "{0} — iniciada em {1}", "{0} — started at {1}");
            Add(t, "shell.interrupted.entryWithFile",
                "{0} — {1}, iniciada em {2}",
                "{0} — {1}, started at {2}");
        }
    }
}
