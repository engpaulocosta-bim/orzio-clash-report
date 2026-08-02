using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace OrzioClashReport.Launcher.Tests;

/// <summary>
/// Reads an assembly's reference tables without loading it, so an architecture assertion never
/// depends on what happens to be resolvable at test time.
/// </summary>
internal static class AssemblyMetadata
{
    /// <summary>Every assembly this assembly references, by simple name.</summary>
    internal static ImmutableHashSet<string> ReferencedAssemblyNames(Assembly assembly)
    {
        return assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .Where(name => name.Length > 0)
            .ToImmutableHashSet(StringComparer.Ordinal);
    }

    /// <summary>
    /// Every external type this assembly names in its metadata, as <c>Namespace.TypeName</c>.
    /// A type the compiler did not emit a reference for is a type the assembly does not use.
    /// </summary>
    internal static ImmutableHashSet<string> ReferencedTypeNames(Assembly assembly)
    {
        string location = assembly.Location;
        Assert.True(File.Exists(location), $"Assembly file not found for '{assembly.GetName().Name}'.");

        using FileStream stream = File.OpenRead(location);
        using var peReader = new PEReader(stream);
        MetadataReader reader = peReader.GetMetadataReader();

        var names = ImmutableHashSet.CreateBuilder<string>(StringComparer.Ordinal);
        foreach (TypeReferenceHandle handle in reader.TypeReferences)
        {
            TypeReference typeReference = reader.GetTypeReference(handle);
            string typeNamespace = reader.GetString(typeReference.Namespace);
            string typeName = reader.GetString(typeReference.Name);
            names.Add(typeNamespace.Length == 0 ? typeName : $"{typeNamespace}.{typeName}");
        }

        return names.ToImmutable();
    }

    internal static string TargetFramework(Assembly assembly)
    {
        var attribute = assembly.GetCustomAttribute<System.Runtime.Versioning.TargetFrameworkAttribute>();
        Assert.NotNull(attribute);
        return attribute!.FrameworkName;
    }
}
