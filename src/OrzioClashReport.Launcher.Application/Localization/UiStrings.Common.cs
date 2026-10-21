using System.Collections.Generic;

namespace OrzioClashReport.Launcher.Application.Localization
{
    internal static partial class UiStrings
    {
        /// <summary>Job panel, common buttons, and the parts every operation form shares.</summary>
        private static void AddJobAndCommon(Dictionary<string, (string Portuguese, string English)> t)
        {
            Add(t, "common.browse", "Escolher…", "Choose…");
            Add(t, "common.open", "Abrir", "Open");
            Add(t, "common.revealInFolder", "Mostrar na pasta", "Show in folder");
            Add(t, "common.chooseAnotherName", "Escolher outro nome", "Choose another name");
            Add(t, "common.replaceExistingReport", "Substituir relatório existente", "Replace existing report");

            // Job panel states. The glyph is separate and language-independent.
            Add(t, "job.state.running", "Em execução", "Running");
            Add(t, "job.state.succeeded", "Concluído", "Completed");
            Add(t, "job.state.canceled", "Cancelado", "Canceled");
            Add(t, "job.state.failed", "Falhou", "Failed");
            Add(t, "job.cancel", "Cancelar", "Cancel");
            Add(t, "job.engineOutput", "Saída do motor", "Engine output");
            Add(t, "job.refusal.busy",
                "Já existe uma operação em curso nesta janela.",
                "An operation is already running in this window.");
            Add(t, "job.refusal.busyNextStep",
                "Espere que termine, ou cancele-a, antes de iniciar outra.",
                "Wait for it to finish, or cancel it, before starting another.");
            Add(t, "job.validation.nextStep",
                "Reveja os campos do formulário e tente novamente.",
                "Review the form fields and try again.");

            // Operation form chrome.
            Add(t, "form.run", "Executar", "Run");
            Add(t, "form.collision.title",
                "Já existe um ficheiro neste destino",
                "A file already exists at this destination");
            Add(t, "form.collision.evidence",
                "Este artefacto é evidência e nunca é substituído. Escolha outro nome.",
                "This artifact is evidence and is never replaced. Choose another name.");
            Add(t, "form.moveUp", "Mover para cima", "Move up");
            Add(t, "form.moveDown", "Mover para baixo", "Move down");
            Add(t, "form.remove", "Remover", "Remove");
            Add(t, "form.add", "Acrescentar…", "Add…");
            Add(t, "form.clear", "Limpar", "Clear");

            // A section whose operations are not wired in this build.
            Add(t, "placeholder.title", "Operações desta secção", "Operations in this section");
            Add(t, "placeholder.body",
                "Estas operações ainda não estão ligadas nesta build. Continuam disponíveis através do motor.",
                "These operations are not wired up in this build. They remain available through the engine.");
        }
    }
}
