using System.Collections.Immutable;
using System.Reflection;
using OrzioClashReport.Launcher.Contracts.Operations;

namespace OrzioClashReport.Launcher.Tests;

/// <summary>
/// The launcher's boundaries, asserted against compiled metadata rather than against documentation.
/// </summary>
public sealed class LauncherArchitectureTests
{
    private static Assembly ContractsAssembly => typeof(LauncherOperationRequest).Assembly;

    private static Assembly ApplicationAssembly =>
        Assembly.Load("OrzioClashReport.Launcher.Application");

    private static Assembly InfrastructureAssembly =>
        Assembly.Load("OrzioClashReport.Launcher.Infrastructure");

    private static Assembly DesktopAssembly =>
        Assembly.Load("OrzioClashReport.Launcher.Desktop");

    public static TheoryData<string> LauncherAssemblyNames => new()
    {
        "OrzioClashReport.Launcher.Contracts",
        "OrzioClashReport.Launcher.Application",
        "OrzioClashReport.Launcher.Infrastructure",
        "OrzioClashReport.Launcher.Desktop",
    };

    [Theory]
    [MemberData(nameof(LauncherAssemblyNames))]
    public void EveryLauncherAssembly_TargetsNet8(string assemblyName)
    {
        Assembly assembly = Assembly.Load(assemblyName);

        Assert.Equal(".NETCoreApp,Version=v8.0", AssemblyMetadata.TargetFramework(assembly));
    }

    [Theory]
    [MemberData(nameof(LauncherAssemblyNames))]
    public void NoLauncherAssembly_ReferencesTheEngine(string assemblyName)
    {
        Assembly assembly = Assembly.Load(assemblyName);

        ImmutableHashSet<string> references = AssemblyMetadata.ReferencedAssemblyNames(assembly);
        string[] engineReferences = references
            .Where(name => name.StartsWith("OrzioClashReport.", StringComparison.Ordinal))
            .Where(name => !name.StartsWith("OrzioClashReport.Launcher.", StringComparison.Ordinal))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(engineReferences);
    }

    [Fact]
    public void Contracts_DoesNotReferenceAvalonia()
    {
        AssertNoAvaloniaReference(ContractsAssembly);
    }

    [Fact]
    public void Application_DoesNotReferenceAvalonia()
    {
        AssertNoAvaloniaReference(ApplicationAssembly);
    }

    [Fact]
    public void Infrastructure_DoesNotReferenceAvalonia()
    {
        AssertNoAvaloniaReference(InfrastructureAssembly);
    }

    [Fact]
    public void Contracts_DoesNotUseProcess()
    {
        AssertDoesNotUseProcess(ContractsAssembly);
    }

    [Fact]
    public void Application_DoesNotUseProcess()
    {
        AssertDoesNotUseProcess(ApplicationAssembly);
    }

    [Fact]
    public void Desktop_DoesNotUseProcess()
    {
        // The user interface never starts a process. Reaching the engine is the gateway's job.
        AssertDoesNotUseProcess(DesktopAssembly);
    }

    [Fact]
    public void Contracts_DependsOnNoOtherLauncherAssembly()
    {
        ImmutableHashSet<string> references = AssemblyMetadata.ReferencedAssemblyNames(ContractsAssembly);

        Assert.Empty(references.Where(name => name.StartsWith("OrzioClashReport.", StringComparison.Ordinal)));
    }

    [Fact]
    public void Application_DependsOnContractsOnly()
    {
        ImmutableHashSet<string> references = AssemblyMetadata.ReferencedAssemblyNames(ApplicationAssembly);
        string[] launcherReferences = references
            .Where(name => name.StartsWith("OrzioClashReport.", StringComparison.Ordinal))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            launcherReferences.Length == 0 ||
            (launcherReferences.Length == 1 && launcherReferences[0] == "OrzioClashReport.Launcher.Contracts"),
            $"Application may depend on Contracts only, but references: {string.Join(", ", launcherReferences)}");
    }

    [Fact]
    public void Infrastructure_DependsOnContractsOnly()
    {
        ImmutableHashSet<string> references = AssemblyMetadata.ReferencedAssemblyNames(InfrastructureAssembly);
        string[] launcherReferences = references
            .Where(name => name.StartsWith("OrzioClashReport.", StringComparison.Ordinal))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            launcherReferences.Length == 0 ||
            (launcherReferences.Length == 1 && launcherReferences[0] == "OrzioClashReport.Launcher.Contracts"),
            $"Infrastructure may depend on Contracts only, but references: {string.Join(", ", launcherReferences)}");
    }

    [Fact]
    public void NoEngineProject_ReferencesTheLauncher()
    {
        string sourceRoot = RepositoryPaths.SourceRoot;
        string[] engineProjects = Directory
            .GetFiles(sourceRoot, "*.csproj", SearchOption.AllDirectories)
            .Where(path => !Path.GetFileName(path).StartsWith("OrzioClashReport.Launcher.", StringComparison.Ordinal))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.NotEmpty(engineProjects);

        foreach (string project in engineProjects)
        {
            string content = File.ReadAllText(project);
            Assert.False(
                content.Contains("OrzioClashReport.Launcher.", StringComparison.Ordinal),
                $"Engine project '{Path.GetFileName(project)}' must not reference the launcher.");
        }
    }

    [Fact]
    public void TestProjects_DoNotMixEngineAndLauncherReferences()
    {
        string testsRoot = Path.Combine(RepositoryPaths.RepositoryRoot, "tests");
        string engineTests = Path.Combine(
            testsRoot, "OrzioClashReport.Tests", "OrzioClashReport.Tests.csproj");

        Assert.True(File.Exists(engineTests), "The engine test project must exist.");
        Assert.DoesNotContain(
            "OrzioClashReport.Launcher.",
            File.ReadAllText(engineTests),
            StringComparison.Ordinal);
    }

    private static void AssertNoAvaloniaReference(Assembly assembly)
    {
        ImmutableHashSet<string> references = AssemblyMetadata.ReferencedAssemblyNames(assembly);
        string[] avalonia = references
            .Where(name => name.StartsWith("Avalonia", StringComparison.Ordinal))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(avalonia);
    }

    private static void AssertDoesNotUseProcess(Assembly assembly)
    {
        ImmutableHashSet<string> types = AssemblyMetadata.ReferencedTypeNames(assembly);

        Assert.DoesNotContain("System.Diagnostics.Process", types);
        Assert.DoesNotContain("System.Diagnostics.ProcessStartInfo", types);
    }
}
