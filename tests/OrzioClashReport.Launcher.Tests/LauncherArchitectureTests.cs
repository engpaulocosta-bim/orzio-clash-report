using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using OrzioClashReport.Launcher.Application.Engine;
using OrzioClashReport.Launcher.Contracts.Operations;
using OrzioClashReport.Launcher.Infrastructure.Platform;

namespace OrzioClashReport.Launcher.Tests
{
    /// <summary>
    /// Enforces the launcher's boundaries as a build-breaking test rather than as documentation. The
    /// two that matter most: the engine must never learn that a launcher exists, and Contracts must
    /// stay free of both the UI framework and the process API.
    /// </summary>
    public sealed class LauncherArchitectureTests
    {
        private const string ContractsProject = "OrzioClashReport.Launcher.Contracts";
        private const string ApplicationProject = "OrzioClashReport.Launcher.Application";
        private const string InfrastructureProject = "OrzioClashReport.Launcher.Infrastructure";
        private const string DesktopProject = "OrzioClashReport.Launcher.Desktop";

        private static readonly string[] EngineProjects =
        {
            "OrzioClashReport.Core",
            "OrzioClashReport.Input.NavisworksXml",
            "OrzioClashReport.Input.RunManifestJson",
            "OrzioClashReport.Output.Html",
            "OrzioClashReport.Persistence.IdentityGovernanceJson",
            "OrzioClashReport.Persistence.ProjectCatalogJson",
            "OrzioClashReport.Persistence.RunIndexJson",
            "OrzioClashReport.Persistence.RunSnapshotJson",
            "OrzioClashReport.Cli",
        };

        [Fact]
        public void NoEngineProjectReferencesTheLauncher()
        {
            foreach (string project in EngineProjects)
            {
                string content = File.ReadAllText(RepositoryLayout.ProjectFile(project));

                Assert.False(
                    content.Contains("Launcher", StringComparison.Ordinal),
                    $"{project} must not reference any launcher project. The engine does not know the launcher exists.");
            }
        }

        [Fact]
        public void EngineTestProjectDoesNotReferenceTheLauncher()
        {
            string content = File.ReadAllText(RepositoryLayout.TestProjectFile("OrzioClashReport.Tests"));

            Assert.False(
                content.Contains("Launcher", StringComparison.Ordinal),
                "The engine test project must stay independent of the launcher.");
        }

        [Fact]
        public void ContractsHasNoProjectOrPackageReferences()
        {
            string content = File.ReadAllText(RepositoryLayout.ProjectFile(ContractsProject));

            Assert.DoesNotContain("<ProjectReference", content, StringComparison.Ordinal);
            Assert.DoesNotContain("<PackageReference", content, StringComparison.Ordinal);
        }

        [Fact]
        public void ApplicationReferencesOnlyContracts()
        {
            IReadOnlyList<string> references = ProjectReferencesOf(ApplicationProject);

            Assert.Equal(new[] { ContractsProject }, references);
            Assert.DoesNotContain(
                "<PackageReference",
                File.ReadAllText(RepositoryLayout.ProjectFile(ApplicationProject)),
                StringComparison.Ordinal);
        }

        [Fact]
        public void InfrastructureReferencesOnlyContractsAndApplication()
        {
            IReadOnlyList<string> references = ProjectReferencesOf(InfrastructureProject);

            Assert.Equal(new[] { ApplicationProject, ContractsProject }, references);
        }

        [Fact]
        public void OnlyTheDesktopProjectReferencesAvalonia()
        {
            foreach (string projectFile in RepositoryLayout.AllProjectFiles())
            {
                string content = File.ReadAllText(projectFile);
                if (!content.Contains("Avalonia", StringComparison.Ordinal))
                {
                    continue;
                }

                Assert.Equal(DesktopProject + ".csproj", Path.GetFileName(projectFile));
            }
        }

        [Fact]
        public void ContractsAndApplicationAssembliesDoNotReferenceAvalonia()
        {
            foreach (Assembly assembly in new[] { ContractsAssembly, ApplicationAssembly })
            {
                foreach (AssemblyName reference in assembly.GetReferencedAssemblies())
                {
                    Assert.False(
                        reference.Name != null && reference.Name.StartsWith("Avalonia", StringComparison.Ordinal),
                        $"{assembly.GetName().Name} must not depend on the UI framework.");
                }
            }
        }

        [Fact]
        public void ContractsAndApplicationSourcesDoNotUseTheProcessApi()
        {
            foreach (string project in new[] { ContractsProject, ApplicationProject })
            {
                string projectDirectory = Path.GetDirectoryName(RepositoryLayout.ProjectFile(project))!;

                foreach (string sourceFile in EnumerateSources(projectDirectory))
                {
                    string content = File.ReadAllText(sourceFile);

                    Assert.False(
                        content.Contains("System.Diagnostics.Process", StringComparison.Ordinal)
                        || content.Contains("ProcessStartInfo", StringComparison.Ordinal)
                        || content.Contains("Process.Start", StringComparison.Ordinal),
                        $"{Path.GetFileName(sourceFile)} in {project} must not touch the process API. "
                        + "Only the infrastructure layer starts processes.");
                }
            }
        }

        [Fact]
        public void NoLauncherSourceStartsAShellIntermediary()
        {
            string[] forbidden = { "cmd.exe", "powershell.exe", "/bin/sh", "UseShellExecute = true", "UseShellExecute=true" };

            foreach (string project in new[] { ContractsProject, ApplicationProject, InfrastructureProject, DesktopProject })
            {
                string projectDirectory = Path.GetDirectoryName(RepositoryLayout.ProjectFile(project))!;

                foreach (string sourceFile in EnumerateSources(projectDirectory))
                {
                    string content = File.ReadAllText(sourceFile);

                    foreach (string needle in forbidden)
                    {
                        Assert.False(
                            content.Contains(needle, StringComparison.Ordinal),
                            $"{Path.GetFileName(sourceFile)} in {project} must not run the engine through a shell "
                            + $"intermediary (found '{needle}').");
                    }
                }
            }
        }

        [Fact]
        public void CoreStillTargetsNetStandard20()
        {
            string content = File.ReadAllText(RepositoryLayout.ProjectFile("OrzioClashReport.Core"));

            Assert.Contains("<TargetFramework>netstandard2.0</TargetFramework>", content, StringComparison.Ordinal);
        }

        [Fact]
        public void EveryLauncherProjectTargetsNet80()
        {
            foreach (string project in new[] { ContractsProject, ApplicationProject, InfrastructureProject, DesktopProject })
            {
                string content = File.ReadAllText(RepositoryLayout.ProjectFile(project));

                Assert.Contains("<TargetFramework>net8.0</TargetFramework>", content, StringComparison.Ordinal);
            }
        }

        [Fact]
        public void EveryOperationDeclaresWhetherItAcceptsAnOutputOption()
        {
            foreach (LauncherOperationKind operation in Enum.GetValues<LauncherOperationKind>())
            {
                // Throws for an operation added without deciding its published CLI shape.
                LauncherOperationMetadata.SupportsOutputOption(operation);
                LauncherOperationMetadata.ProducesReplaceableHtmlOutput(operation);
            }
        }

        private static Assembly ContractsAssembly => typeof(LauncherOperationKind).Assembly;

        private static Assembly ApplicationAssembly => typeof(EngineVersionParser).Assembly;

        private static Assembly InfrastructureAssembly => typeof(SystemClock).Assembly;

        [Fact]
        public void InfrastructureDependsOnContracts()
        {
            Assert.Contains(
                InfrastructureAssembly.GetReferencedAssemblies(),
                reference => reference.Name == ContractsAssembly.GetName().Name);
        }

        private static IReadOnlyList<string> ProjectReferencesOf(string project)
        {
            string content = File.ReadAllText(RepositoryLayout.ProjectFile(project));

            return content
                .Split('\n')
                .Where(line => line.Contains("<ProjectReference", StringComparison.Ordinal))
                .Select(line =>
                {
                    int start = line.IndexOf("Include=\"", StringComparison.Ordinal) + "Include=\"".Length;
                    int end = line.IndexOf('"', start);

                    // Project files always use Windows separators; normalise so the test reads the
                    // same on any host operating system.
                    string include = line.Substring(start, end - start).Replace('\\', '/');
                    return Path.GetFileNameWithoutExtension(include);
                })
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();
        }

        private static IEnumerable<string> EnumerateSources(string projectDirectory)
        {
            return Directory
                .EnumerateFiles(projectDirectory, "*.cs", SearchOption.AllDirectories)
                .Where(path => !path.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                .Where(path => !path.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.Ordinal));
        }
    }
}
