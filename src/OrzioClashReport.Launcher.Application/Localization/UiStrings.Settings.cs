using System.Collections.Generic;

namespace OrzioClashReport.Launcher.Application.Localization
{
    internal static partial class UiStrings
    {
        /// <summary>Settings, the update flow, and the diagnostic bundle.</summary>
        private static void AddSettingsAndUpdates(Dictionary<string, (string Portuguese, string English)> t)
        {
            Add(t, "settings.subtitle",
                "Aparência, atualizações, dados locais e estado do motor.",
                "Appearance, updates, local data and engine status.");

            // Updates card.
            Add(t, "settings.updates.title", "Atualizações", "Updates");
            Add(t, "settings.updates.body",
                "Procura uma nova versão oficial no GitHub apenas quando o pedir. "
                + "A instalação só começa com a sua confirmação.",
                "Looks for a new official version on GitHub only when you ask. "
                + "Installation only starts with your confirmation.");

            Add(t, "updates.check", "Procurar atualizações", "Check for updates");
            Add(t, "updates.install", "Instalar atualização {0}", "Install update {0}");
            Add(t, "updates.releaseNotes", "Notas da versão", "Release notes");
            Add(t, "updates.downloaded", "{0}% descarregado", "{0}% downloaded");

            Add(t, "updates.status.idle",
                "Ainda não procurou atualizações nesta sessão.",
                "You have not checked for updates in this session yet.");
            Add(t, "updates.status.checking",
                "A procurar atualizações oficiais no GitHub…",
                "Checking GitHub for official updates…");
            Add(t, "updates.status.upToDate",
                "Tem a versão mais recente ({0}).",
                "You have the latest version ({0}).");
            Add(t, "updates.status.available",
                "Nova versão disponível: {0}. A instalação só começa quando a pedir.",
                "New version available: {0}. Installation only starts when you ask.");
            Add(t, "updates.status.downloading",
                "A descarregar a versão {0}…",
                "Downloading version {0}…");
            Add(t, "updates.status.installing",
                "Instalador verificado (SHA-256) e iniciado. "
                + "A aplicação vai fechar para a atualização poder substituir os ficheiros.",
                "Installer verified (SHA-256) and started. "
                + "The application will close so the update can replace the files.");
            Add(t, "updates.status.lastChecked", "Última verificação: {0}", "Last checked: {0}");

            Add(t, "updates.sidebar.checking", "A procurar…", "Checking…");
            Add(t, "updates.sidebar.upToDate", "Atualizado · {0}", "Up to date · {0}");
            Add(t, "updates.sidebar.downloading", "A descarregar…", "Downloading…");
            Add(t, "updates.sidebar.installing", "A instalar…", "Installing…");
            Add(t, "updates.sidebar.failed", "Verificação falhou", "Check failed");
            Add(t, "updates.sidebar.version", "Versão {0}", "Version {0}");

            Add(t, "updates.error.rateLimited",
                "O GitHub recusou temporariamente o pedido (limite de pedidos). "
                + "Tente novamente dentro de alguns minutos.",
                "GitHub temporarily refused the request (rate limit). Try again in a few minutes.");
            Add(t, "updates.error.unreachable",
                "Não foi possível contactar o GitHub. Verifique a ligação à internet e tente novamente.",
                "Could not reach GitHub. Check your internet connection and try again.");
            Add(t, "updates.error.installerMismatch",
                "O instalador descarregado não corresponde ao SHA-256 publicado e não foi executado.",
                "The downloaded installer does not match the published SHA-256 and was not run.");
            Add(t, "updates.error.releaseRejected",
                "A release publicada não passou a validação de segurança e foi ignorada.",
                "The published release did not pass the security validation and was ignored.");
            Add(t, "updates.error.unsupportedPlatform",
                "A instalação automática só está disponível no Windows.",
                "Automatic installation is only available on Windows.");
            Add(t, "updates.error.generic",
                "Não foi possível concluir a atualização: {0}",
                "Could not complete the update: {0}");

            // Appearance.
            Add(t, "settings.appearance.title", "Aparência", "Appearance");
            Add(t, "settings.language.label", "Idioma", "Language");
            Add(t, "settings.language.system", "Sistema (automático)", "System (automatic)");
            Add(t, "settings.language.portuguese", "Português", "Português");
            Add(t, "settings.language.english", "English", "English");
            Add(t, "settings.theme.label", "Tema", "Theme");
            Add(t, "settings.theme.system", "Sistema", "System");
            Add(t, "settings.theme.light", "Claro", "Light");
            Add(t, "settings.theme.dark", "Escuro", "Dark");
            Add(t, "settings.experimentalWarnings",
                "Mostrar avisos em funcionalidades experimentais",
                "Show warnings on experimental features");

            // Engine.
            Add(t, "settings.engine.title", "Motor", "Engine");
            Add(t, "settings.engine.reportedVersion", "Versão reportada", "Reported version");
            Add(t, "settings.engine.integrity", "Integridade", "Integrity");
            Add(t, "settings.engine.applicationVersion", "Versão da aplicação", "Application version");
            Add(t, "settings.integrity.verified",
                "SHA-256 verificado contra o manifesto instalado.",
                "SHA-256 verified against the installed manifest.");
            Add(t, "settings.integrity.mismatch",
                "SHA-256 diferente do manifesto instalado.",
                "SHA-256 differs from the installed manifest.");
            Add(t, "settings.integrity.manifestUnavailable",
                "Manifesto do motor indisponível: integridade não verificada.",
                "Engine manifest unavailable: integrity not verified.");
            Add(t, "settings.integrity.notChecked",
                "Integridade ainda não verificada.",
                "Integrity not verified yet.");

            // Local data.
            Add(t, "settings.data.title", "Dados locais", "Local data");
            Add(t, "settings.data.body",
                "Definições, lista de recentes, registos e jobs ficam na sua pasta de utilizador. "
                + "Os relatórios e ficheiros de projeto ficam onde os guardar, e a desinstalação não lhes toca.",
                "Settings, the recent list, logs and jobs stay in your user folder. "
                + "Reports and project files stay where you save them, and uninstalling never touches them.");
            Add(t, "settings.data.open", "Abrir pasta de dados", "Open data folder");
            Add(t, "settings.data.clearRecent", "Limpar lista de recentes", "Clear recent list");
            Add(t, "settings.data.recentCleared",
                "Lista de relatórios recentes limpa. Nenhum ficheiro foi apagado.",
                "Recent reports list cleared. No file was deleted.");

            // Diagnostics.
            Add(t, "settings.diagnostics.title", "Diagnóstico", "Diagnostics");
            Add(t, "settings.diagnostics.body",
                "Um pacote de diagnóstico é criado apenas quando o pedir, fica na sua pasta de dados "
                + "e não é enviado para lado nenhum.",
                "A diagnostic bundle is only created when you ask, stays in your data folder "
                + "and is not sent anywhere.");
            Add(t, "settings.diagnostics.preview", "Ver o que seria incluído", "See what would be included");
            Add(t, "settings.diagnostics.contents",
                "O pacote contém exatamente estes ficheiros",
                "The bundle contains exactly these files");
            Add(t, "settings.diagnostics.excluded",
                "Nenhum ficheiro seu é incluído: nem XML, nem manifest, nem snapshot, nem governança, nem relatórios.",
                "None of your files is included: no XML, no manifest, no snapshot, no governance, no reports.");
            Add(t, "settings.diagnostics.logPreview",
                "Pré-visualização do registo redigido",
                "Redacted log preview");
            Add(t, "settings.diagnostics.create", "Gerar pacote", "Create bundle");
            Add(t, "settings.diagnostics.waitForEngine",
                "Espere que a verificação do motor termine antes de gerar o diagnóstico.",
                "Wait for the engine check to finish before creating the diagnostics.");
            Add(t, "settings.diagnostics.created",
                "Pacote de diagnóstico criado em {0}.",
                "Diagnostic bundle created in {0}.");
            Add(t, "settings.diagnostics.failed",
                "Não foi possível criar o pacote de diagnóstico: {0}",
                "Could not create the diagnostic bundle: {0}");
            Add(t, "settings.diagnostics.noPermission",
                "Não foi possível criar o pacote de diagnóstico: sem permissão de escrita.",
                "Could not create the diagnostic bundle: no write permission.");

            // What each allow-listed bundle file is. Keyed by file name: the set is closed.
            Add(t, "diagnostics.item.launcher-version.json",
                "A versão desta aplicação.",
                "This application's version.");
            Add(t, "diagnostics.item.engine-info.json",
                "O estado do motor: versão reportada, versão esperada e resultado da verificação.",
                "The engine's state: reported version, expected version and the result of the check.");
            Add(t, "diagnostics.item.operating-system.json",
                "O sistema operativo e a arquitetura. Sem nome de máquina e sem nome de utilizador.",
                "The operating system and architecture. No machine name and no user name.");
            Add(t, "diagnostics.item.job-summary.json",
                "Um resumo das operações recentes: tipo, estado e código de saída.",
                "A summary of recent operations: type, state and exit code.");
            Add(t, "diagnostics.item.redacted-log.jsonl",
                "O registo local já redigido. Os caminhos aparecem apenas como nome de ficheiro, extensão e hash.",
                "The local log, already redacted. Paths appear only as file name, extension and hash.");
            Add(t, "diagnostics.item.integrity-check.json",
                "O resultado da verificação SHA-256 do motor.",
                "The result of the engine's SHA-256 check.");

            // What the application deliberately does not do.
            Add(t, "settings.promise.title",
                "O que esta aplicação não faz",
                "What this application does not do");
            Add(t, "settings.promise.body",
                "Não recolhe telemetria e não envia os seus ficheiros. Funciona offline, exceto ao procurar "
                + "ou descarregar atualizações a pedido do utilizador. Os registos locais não guardam "
                + "caminhos absolutos nem conteúdo dos seus ficheiros.",
                "It collects no telemetry and sends none of your files. It works offline, except when "
                + "checking for or downloading updates at the user's request. Local logs do not store "
                + "absolute paths or the contents of your files.");
        }
    }
}
