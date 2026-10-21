using System.Collections.Generic;

namespace OrzioClashReport.Launcher.Application.Localization
{
    internal static partial class UiStrings
    {
        /// <summary>The home screen, the quick report, and the file pickers.</summary>
        private static void AddHomeAndQuickReport(Dictionary<string, (string Portuguese, string English)> t)
        {
            Add(t, "home.overline",
                "COORDENAÇÃO BIM · NAVISWORKS CLASH DETECTIVE",
                "BIM COORDINATION · NAVISWORKS CLASH DETECTIVE");
            Add(t, "home.hero.title",
                "Do export de clashes a um relatório pronto para a reunião.",
                "From a clash export to a report ready for the meeting.");
            Add(t, "home.hero.body",
                "Transforma exports do Navisworks Clash Detective em relatórios de coordenação, "
                + "comparações entre revisões e decisões humanas explícitas.",
                "Turns Navisworks Clash Detective exports into coordination reports, "
                + "comparisons between revisions and explicit human decisions.");
            Add(t, "home.hero.quickReport", "Gerar relatório rápido", "Generate quick report");
            Add(t, "home.hero.compare", "Comparar revisões", "Compare revisions");

            Add(t, "home.update.title", "Nova versão disponível: {0}", "New version available: {0}");
            Add(t, "home.update.body",
                "A instalação só começa quando a confirmar em Definições.",
                "Installation only starts when you confirm it in Settings.");
            Add(t, "home.update.view", "Ver atualização", "View update");

            Add(t, "home.start", "Começar", "Get started");

            Add(t, "home.tile.quick.body",
                "Um export XML para um relatório HTML agrupado. Não precisa de manifest.",
                "One XML export to one grouped HTML report. No manifest needed.");

            Add(t, "home.tile.snapshot.title", "Criar snapshot", "Create snapshot");
            Add(t, "home.tile.snapshot.body",
                "Congela um run como evidência imutável, a partir do XML e do manifest declarado.",
                "Freezes a run as immutable evidence, from the XML and the declared manifest.");
            Add(t, "home.tile.snapshot.action", "Ir para Snapshots", "Go to Snapshots");

            Add(t, "home.tile.compare.body",
                "Compara pares adjacentes na ordem que declarar. Nada é ordenado por data, nome ou revisão.",
                "Compares adjacent pairs in the order you declare. Nothing is sorted by date, name or revision.");
            Add(t, "home.tile.compare.action", "Ir para Longitudinal", "Go to Longitudinal");

            Add(t, "home.recent.title", "Últimos relatórios", "Latest reports");
            Add(t, "home.recent.empty",
                "Ainda não gerou nenhum ficheiro. O que gerar aparece aqui, e fica onde o guardou.",
                "You have not generated any file yet. What you generate appears here, "
                + "and stays where you saved it.");

            // Quick report.
            Add(t, "quick.subtitle",
                "Um export XML do Clash Detective para um relatório HTML agrupado. "
                + "Não precisa de manifest nem de projeto.",
                "One Clash Detective XML export to one grouped HTML report. "
                + "No manifest or project needed.");
            Add(t, "quick.input.title", "Ficheiro de entrada", "Input file");
            Add(t, "quick.input.description",
                "O XML exportado pelo Clash Detective do Navisworks.",
                "The XML exported by Navisworks Clash Detective.");
            Add(t, "quick.output.title", "Destino do relatório", "Report destination");
            Add(t, "quick.output.description",
                "Onde o ficheiro HTML será escrito. Fica sempre onde o escolher.",
                "Where the HTML file will be written. It always stays where you choose.");
            Add(t, "quick.generate", "Gerar relatório", "Generate report");
            Add(t, "quick.collision.title",
                "Já existe um relatório neste destino",
                "A report already exists at this destination");
            Add(t, "quick.collision.body",
                "O relatório é derivado e pode ser gerado de novo, por isso pode ser substituído. "
                + "Nada mais é substituído por esta aplicação.",
                "The report is derived and can be generated again, so it can be replaced. "
                + "This application replaces nothing else.");

            // File pickers.
            Add(t, "picker.title.chooseXml",
                "Escolher o export XML do Clash Detective",
                "Choose the Clash Detective XML export");
            Add(t, "picker.title.chooseReportDestination",
                "Escolher onde guardar o relatório",
                "Choose where to save the report");
            Add(t, "picker.title.chooseDestination", "Escolher destino", "Choose destination");
            Add(t, "picker.title.chooseFile", "Escolher ficheiro", "Choose file");
            Add(t, "picker.title.chooseFiles", "Escolher ficheiros", "Choose files");

            Add(t, "picker.watermark.noFile", "Nenhum ficheiro escolhido", "No file chosen");
            Add(t, "picker.watermark.noDestination", "Nenhum destino escolhido", "No destination chosen");

            Add(t, "picker.filter.all", "Todos os ficheiros", "All files");
            Add(t, "picker.filter.clashXml", "Export XML do Clash Detective", "Clash Detective XML export");
            Add(t, "picker.filter.htmlReport", "Relatório HTML", "HTML report");
            Add(t, "picker.filter.runManifest", "Run manifest JSON", "Run manifest JSON");
            Add(t, "picker.filter.runSnapshot", "Run snapshot JSON", "Run snapshot JSON");
            Add(t, "picker.filter.runIndex", "Run index JSON", "Run index JSON");
            Add(t, "picker.filter.projectCatalog", "Catálogo de projeto JSON", "Project catalog JSON");
            Add(t, "picker.filter.identityGovernance",
                "Governança de identidade JSON",
                "Identity governance JSON");
        }
    }
}
