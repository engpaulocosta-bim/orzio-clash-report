using System;
using System.IO;
using OrzioClashReport.Launcher.Contracts.Logging;
using OrzioClashReport.Launcher.Infrastructure.Logging;

namespace OrzioClashReport.Launcher.Tests
{
    public sealed class Sha256PathRedactorTests
    {
        [Fact]
        public void KeepsOnlyTheFileNameExtensionHashAndRootKind()
        {
            string root = Path.Combine(Path.GetTempPath(), "orzio-redactor-tests");
            var redactor = new Sha256PathRedactor(
                localApplicationDataRoot: Path.Combine(root, "local"),
                userProfileRoot: Path.Combine(root, "profile"),
                installationRoot: Path.Combine(root, "install"),
                temporaryRoot: Path.Combine(root, "temp"));

            string path = Path.Combine(root, "profile", "Clients", "ACME Tower", "run-004.xml");

            RedactedPath redacted = redactor.Redact(path);

            Assert.Equal("run-004.xml", redacted.FileName);
            Assert.Equal(".xml", redacted.Extension);
            Assert.Equal(PathRootKind.UserProfile, redacted.RootKind);
            Assert.Equal(64, redacted.PathHash.Length);
        }

        [Fact]
        public void NeverExposesTheDirectoryStructureOrTheClientName()
        {
            var redactor = new Sha256PathRedactor();
            string path = Path.Combine("Clients", "ACME Tower", "Coordination", "report.html");

            RedactedPath redacted = redactor.Redact(path);

            Assert.DoesNotContain("ACME", redacted.FileName, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("ACME", redacted.PathHash, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Clients", redacted.PathHash, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void HashIsStableForTheSamePathAndDifferentForAnother()
        {
            var redactor = new Sha256PathRedactor();

            string first = redactor.Redact(Path.Combine("a", "b", "report.html")).PathHash;
            string second = redactor.Redact(Path.Combine("a", "b", "report.html")).PathHash;
            string other = redactor.Redact(Path.Combine("a", "c", "report.html")).PathHash;

            Assert.Equal(first, second);
            Assert.NotEqual(first, other);
        }

        [Fact]
        public void ClassifiesTheMostSpecificRootFirst()
        {
            string root = Path.Combine(Path.GetTempPath(), "orzio-redactor-roots");
            var redactor = new Sha256PathRedactor(
                localApplicationDataRoot: Path.Combine(root, "profile", "AppData", "Local"),
                userProfileRoot: Path.Combine(root, "profile"),
                installationRoot: Path.Combine(root, "profile", "AppData", "Local", "Programs", "Orzio"),
                temporaryRoot: Path.Combine(root, "temp"));

            Assert.Equal(
                PathRootKind.InstallationDirectory,
                redactor.Redact(Path.Combine(root, "profile", "AppData", "Local", "Programs", "Orzio", "engine", "orzioclash.exe")).RootKind);

            Assert.Equal(
                PathRootKind.LocalApplicationData,
                redactor.Redact(Path.Combine(root, "profile", "AppData", "Local", "Orzio", "settings.json")).RootKind);

            Assert.Equal(
                PathRootKind.UserProfile,
                redactor.Redact(Path.Combine(root, "profile", "Documents", "report.html")).RootKind);

            Assert.Equal(
                PathRootKind.TemporaryDirectory,
                redactor.Redact(Path.Combine(root, "temp", "scratch.html")).RootKind);
        }

        [Fact]
        public void RejectsANullPath()
        {
            var redactor = new Sha256PathRedactor();

            Assert.Throws<ArgumentNullException>(() => redactor.Redact(null!));
        }
    }
}
