using System.Collections.Generic;

namespace OrzioClashReport.Launcher.Application.Localization
{
    internal static partial class UiStrings
    {
        /// <summary>
        /// Messages the launcher itself produces about the engine, a request, or a form. They carry what
        /// failed and what to do next, and are shown in the language active when they were produced.
        /// The engine's own output is never in this table: it is shown exactly as the engine wrote it.
        /// </summary>
        private static void AddMessages(Dictionary<string, (string Portuguese, string English)> t)
        {
            // Running the engine.
            Add(t, "failure.engine.outputTruncated",
                "O motor produziu mais texto do que é mostrado. Foram guardados o início e o fim.",
                "The engine produced more text than is shown. The beginning and the end were kept.");

            Add(t, "failure.engine.startFailure.message",
                "O motor não pôde ser iniciado.",
                "The engine could not be started.");
            Add(t, "failure.engine.startFailure.nextStep",
                "Verifique o estado do motor em Definições e reinstale se necessário.",
                "Check the engine status in Settings and reinstall if necessary.");

            Add(t, "failure.engine.timeout.message",
                "O motor não terminou dentro do tempo permitido e foi interrompido.",
                "The engine did not finish within the allowed time and was stopped.");
            Add(t, "failure.engine.timeout.nextStep",
                "Tente novamente com um export mais pequeno, ou verifique se o ficheiro de entrada está acessível.",
                "Try again with a smaller export, or check that the input file is accessible.");

            Add(t, "failure.engine.executionFailure.message",
                "O motor terminou com um erro.",
                "The engine finished with an error.");
            Add(t, "failure.engine.executionFailure.nextStep",
                "Leia a saída do motor abaixo: descreve exatamente o que falhou.",
                "Read the engine output below: it describes exactly what failed.");

            Add(t, "failure.engine.outputMissing.message",
                "O motor terminou com sucesso mas o ficheiro de destino não existe ou está vazio.",
                "The engine finished successfully but the destination file does not exist or is empty.");
            Add(t, "failure.engine.outputMissing.nextStep",
                "Confirme que tem permissão de escrita no destino e tente novamente.",
                "Check that you have write permission at the destination and try again.");

            // The engine cannot be used at all.
            Add(t, "failure.unavailable.missing.message",
                "Não existe motor instalado.",
                "No engine is installed.");
            Add(t, "failure.unavailable.missing.nextStep",
                "Reinstale a aplicação para repor o motor.",
                "Reinstall the application to restore the engine.");

            Add(t, "failure.unavailable.integrity.message",
                "O motor instalado não passou a verificação de integridade.",
                "The installed engine did not pass the integrity check.");
            Add(t, "failure.unavailable.integrity.nextStep",
                "Reinstale a aplicação. Nenhuma operação é executada com um motor por verificar.",
                "Reinstall the application. No operation runs with an unverified engine.");

            Add(t, "failure.unavailable.version.message",
                "O motor instalado reporta {0}, mas esta aplicação foi empacotada com {1}.",
                "The installed engine reports {0}, but this application was packaged with {1}.");
            Add(t, "failure.unavailable.version.nextStep",
                "Reinstale a aplicação para voltar a um par verificado.",
                "Reinstall the application to return to a verified pair.");

            Add(t, "failure.unavailable.generic.message",
                "O motor não está disponível.",
                "The engine is not available.");
            Add(t, "failure.unavailable.generic.nextStep",
                "Verifique o estado do motor em Definições.",
                "Check the engine status in Settings.");

            // Checking the engine at startup.
            Add(t, "probe.notFound",
                "Não foi encontrado nenhum executável do motor na pasta de instalação.",
                "No engine executable was found in the installation folder.");
            Add(t, "probe.integrityMismatch",
                "O executável do motor não corresponde ao SHA-256 registado na instalação.",
                "The engine executable does not match the SHA-256 recorded at installation.");
            Add(t, "probe.manifestMissing",
                "Não foi possível verificar a integridade do motor: falta o manifesto empacotado.",
                "The engine's integrity could not be verified: the packaged manifest is missing.");
            Add(t, "probe.startFailure",
                "O motor não pôde ser iniciado neste sistema.",
                "The engine could not be started on this system.");
            Add(t, "probe.timeout",
                "O motor não respondeu a --version dentro de cinco segundos.",
                "The engine did not answer --version within five seconds.");
            Add(t, "probe.interrupted",
                "A verificação do motor foi interrompida.",
                "The engine check was interrupted.");
            Add(t, "probe.exitCode",
                "O motor terminou com o código {0} ao responder a --version.",
                "The engine exited with code {0} when answering --version.");
            Add(t, "probe.unexpectedFormat",
                "O motor não respondeu no formato publicado de --version.",
                "The engine did not answer in the published --version format.");
            Add(t, "probe.versionMismatch",
                "O motor reporta {0}, mas esta aplicação foi empacotada com {1}.",
                "The engine reports {0}, but this application was packaged with {1}.");
            Add(t, "probe.ready", "Motor {0} verificado.", "Engine {0} verified.");

            // Refusing a request before the engine is started.
            Add(t, "failure.request.noArguments.message",
                "A operação não tem argumentos.",
                "The operation has no arguments.");
            Add(t, "failure.request.noArguments.nextStep",
                "Preencha o formulário e tente novamente.",
                "Fill in the form and try again.");

            Add(t, "failure.request.workingDirectoryMissing.message",
                "A pasta de trabalho da operação não existe.",
                "The operation's working folder does not exist.");
            Add(t, "failure.request.workingDirectoryMissing.nextStep",
                "Escolha um destino cuja pasta já exista.",
                "Choose a destination whose folder already exists.");

            Add(t, "failure.request.insideInstallation.message",
                "A operação não pode escrever dentro da pasta de instalação.",
                "The operation cannot write inside the installation folder.");
            Add(t, "failure.request.insideInstallation.nextStep",
                "Escolha um destino na sua pasta de trabalho ou de projeto.",
                "Choose a destination in your working or project folder.");

            Add(t, "failure.request.notAbsolute.message",
                "O destino tem de ser um caminho absoluto.",
                "The destination must be an absolute path.");
            Add(t, "failure.request.notAbsolute.nextStep",
                "Escolha o destino através do seletor de ficheiros.",
                "Choose the destination with the file picker.");

            Add(t, "failure.request.collisionEvidence.message",
                "Já existe um ficheiro neste destino e este tipo de artefacto nunca é substituído.",
                "A file already exists at this destination and this kind of artifact is never replaced.");
            Add(t, "failure.request.collisionEvidence.nextStep",
                "Escolha outro nome. Snapshots, índices, catálogos e governança são sempre criados de novo.",
                "Choose another name. Snapshots, indexes, catalogs and governance are always created anew.");

            Add(t, "failure.request.collisionReport.message",
                "Já existe um relatório neste destino.",
                "A report already exists at this destination.");
            Add(t, "failure.request.collisionReport.nextStep",
                "Escolha outro nome ou confirme explicitamente que quer substituir o relatório existente.",
                "Choose another name or explicitly confirm that you want to replace the existing report.");

            // A decision that cannot be recorded as typed.
            Add(t, "draft.error.decisionId",
                "O identificador da decisão é obrigatório.",
                "The decision identifier is required.");
            Add(t, "draft.error.leftRunId",
                "O run id do lado esquerdo é obrigatório.",
                "The left-hand run id is required.");
            Add(t, "draft.error.rightRunId",
                "O run id do lado direito é obrigatório.",
                "The right-hand run id is required.");
            Add(t, "draft.error.reviewerAlias",
                "O alias do revisor é obrigatório.",
                "The reviewer alias is required.");
            Add(t, "draft.error.confirmNeedsIdentity",
                "Uma confirmação exige um identificador persistente.",
                "A confirmation requires a persistent identifier.");
            Add(t, "draft.error.rejectCarriesIdentity",
                "Uma rejeição nunca pode transportar um identificador persistente.",
                "A rejection can never carry a persistent identifier.");
            Add(t, "draft.error.negativeOccurrenceIndex",
                "O índice de ocorrência é zero-based e nunca pode ser negativo.",
                "The occurrence index is zero-based and can never be negative.");

            // An ordered list of files.
            Add(t, "orderedFiles.duplicate",
                "A posição {0} repete a posição {1} ({2}). A repetição é mantida exatamente como declarada.",
                "Position {0} repeats position {1} ({2}). The repetition is kept exactly as declared.");
        }
    }
}
