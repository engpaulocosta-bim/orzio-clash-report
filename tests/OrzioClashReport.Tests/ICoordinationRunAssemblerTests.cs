using System;
using System.Reflection;
using System.Threading;
using OrzioClashReport.Core.Abstractions;
using OrzioClashReport.Core.Assembly;
using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Tests
{
    /// <summary>Locks the public shape of ICoordinationRunAssembler and proves it is implementable end to end.</summary>
    public class ICoordinationRunAssemblerTests
    {
        /// <summary>Test-only fake: a private type implementing ICoordinationRunAssembler by delegating to a real assembler, proving the interface is implementable outside the Core assembly's internals.</summary>
        private sealed class DelegatingCoordinationRunAssembler : ICoordinationRunAssembler
        {
            private readonly ICoordinationRunAssembler _inner;

            public DelegatingCoordinationRunAssembler(ICoordinationRunAssembler inner)
            {
                _inner = inner;
            }

            public CoordinationRun Assemble(ClashReportDocument document, RunManifest manifest) =>
                _inner.Assemble(document, manifest);
        }

        private static ModelRevision MakeRevision(string company, string discipline, string modelName, string revision, string sourceFileName) =>
            new ModelRevision(new ModelIdentity(company, discipline, modelName), revision, sourceFileName, null, null, null);

        private static RunManifest MakeEmptyManifest(string runId) =>
            new RunManifest(
                runId,
                new DateTimeOffset(2026, 7, 10, 9, 0, 0, TimeSpan.Zero),
                new[] { MakeRevision("Sigma", "Structure", "Main", "R04", "Sigma_Main_R04.nwc") },
                Array.Empty<ExecutedClashTest>());

        // --- Public shape, via reflection ---

        [Fact]
        public void ICoordinationRunAssembler_IsPublicInterface()
        {
            var type = typeof(ICoordinationRunAssembler);

            Assert.True(type.IsInterface);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void ICoordinationRunAssembler_DeclaresExactlyOnePublicMethod()
        {
            var methods = typeof(ICoordinationRunAssembler).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            Assert.Single(methods);
        }

        [Fact]
        public void ICoordinationRunAssembler_MethodIsNamedAssemble()
        {
            var method = typeof(ICoordinationRunAssembler).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)[0];

            Assert.Equal("Assemble", method.Name);
        }

        [Fact]
        public void ICoordinationRunAssembler_ReturnTypeIsCoordinationRun()
        {
            var method = typeof(ICoordinationRunAssembler).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)[0];

            Assert.Equal(typeof(CoordinationRun), method.ReturnType);
        }

        [Fact]
        public void ICoordinationRunAssembler_HasExactlyTwoParameters()
        {
            var method = typeof(ICoordinationRunAssembler).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)[0];

            Assert.Equal(2, method.GetParameters().Length);
        }

        [Fact]
        public void ICoordinationRunAssembler_FirstParameterIsNamedDocument()
        {
            var method = typeof(ICoordinationRunAssembler).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)[0];

            Assert.Equal("document", method.GetParameters()[0].Name);
        }

        [Fact]
        public void ICoordinationRunAssembler_SecondParameterIsNamedManifest()
        {
            var method = typeof(ICoordinationRunAssembler).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)[0];

            Assert.Equal("manifest", method.GetParameters()[1].Name);
        }

        [Fact]
        public void ICoordinationRunAssembler_FirstParameterIsClashReportDocument()
        {
            var method = typeof(ICoordinationRunAssembler).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)[0];

            Assert.Equal(typeof(ClashReportDocument), method.GetParameters()[0].ParameterType);
        }

        [Fact]
        public void ICoordinationRunAssembler_SecondParameterIsRunManifest()
        {
            var method = typeof(ICoordinationRunAssembler).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)[0];

            Assert.Equal(typeof(RunManifest), method.GetParameters()[1].ParameterType);
        }

        [Fact]
        public void ICoordinationRunAssembler_DoesNotAcceptACollection()
        {
            var method = typeof(ICoordinationRunAssembler).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)[0];

            Assert.DoesNotContain(method.GetParameters(), p => typeof(System.Collections.IEnumerable).IsAssignableFrom(p.ParameterType));
        }

        [Fact]
        public void ICoordinationRunAssembler_IsNotAsync()
        {
            var method = typeof(ICoordinationRunAssembler).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)[0];

            Assert.False(typeof(System.Threading.Tasks.Task).IsAssignableFrom(method.ReturnType));
        }

        [Fact]
        public void ICoordinationRunAssembler_DoesNotAcceptCancellationToken()
        {
            var method = typeof(ICoordinationRunAssembler).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)[0];

            Assert.DoesNotContain(method.GetParameters(), p => p.ParameterType == typeof(CancellationToken));
        }

        // --- Implementability ---

        [Fact]
        public void ICoordinationRunAssembler_IsImplementableOutsideCore_ByDelegation()
        {
            ICoordinationRunAssembler fake = new DelegatingCoordinationRunAssembler(new ExactSourceModelCoordinationRunAssembler());
            var manifest = MakeEmptyManifest("run-1");
            var document = new ClashReportDocument(null, null, Array.Empty<ClashBatch>());

            var run = fake.Assemble(document, manifest);

            Assert.NotNull(run);
            Assert.Empty(run.Occurrences);
        }
    }
}
