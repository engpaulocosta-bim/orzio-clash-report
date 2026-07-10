using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using OrzioClashReport.Core.Abstractions;
using OrzioClashReport.Core.Matching;
using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Tests
{
    /// <summary>Locks the public shape of IClashRunComparer and proves it is implementable end to end.</summary>
    public class IClashRunComparerTests
    {
        private static ModelRevision MakeRevision(string company, string discipline, string modelName, string revision) =>
            new ModelRevision(new ModelIdentity(company, discipline, modelName), revision, $"{modelName}_{revision}.nwc", null, null, null);

        private static RunManifest MakeManifest(string runId, params ModelRevision[] models) =>
            new RunManifest(runId, new DateTimeOffset(2026, 7, 10, 9, 0, 0, TimeSpan.Zero), models);

        private static CoordinationRun MakeEmptyRun(string runId) =>
            new CoordinationRun(MakeManifest(runId, MakeRevision("Sigma", "Structure", "Main", "R04")), Array.Empty<ClashOccurrence>());

        /// <summary>Never produces a candidate. Used only to keep the delegating fake below simple.</summary>
        private sealed class NullClashMatcher : IClashMatcher
        {
            public ClashMatchAssessment? Assess(ClashOccurrence previousOccurrence, ClashOccurrence currentOccurrence)
            {
                if (previousOccurrence == null)
                {
                    throw new ArgumentNullException(nameof(previousOccurrence));
                }

                if (currentOccurrence == null)
                {
                    throw new ArgumentNullException(nameof(currentOccurrence));
                }

                return null;
            }
        }

        /// <summary>Test-only fake: a private type implementing IClashRunComparer by delegating to a real comparer, proving the interface is implementable outside the Core assembly's internals.</summary>
        private sealed class DelegatingClashRunComparer : IClashRunComparer
        {
            private readonly IClashRunComparer _inner;

            public DelegatingClashRunComparer(IClashRunComparer inner)
            {
                _inner = inner;
            }

            public ClashRunMatchResult Compare(CoordinationRun previousRun, CoordinationRun currentRun) =>
                _inner.Compare(previousRun, currentRun);
        }

        // --- Public shape, via reflection ---

        [Fact]
        public void IClashRunComparer_IsPublicInterface()
        {
            var type = typeof(IClashRunComparer);

            Assert.True(type.IsInterface);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void IClashRunComparer_DeclaresExactlyOnePublicMethod()
        {
            var methods = typeof(IClashRunComparer).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            Assert.Single(methods);
        }

        [Fact]
        public void IClashRunComparer_MethodIsNamedCompare()
        {
            var method = typeof(IClashRunComparer).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Single();

            Assert.Equal("Compare", method.Name);
        }

        [Fact]
        public void IClashRunComparer_ReturnTypeIsClashRunMatchResult()
        {
            var method = typeof(IClashRunComparer).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Single();

            Assert.Equal(typeof(ClashRunMatchResult), method.ReturnType);
        }

        [Fact]
        public void IClashRunComparer_HasExactlyTwoParameters()
        {
            var method = typeof(IClashRunComparer).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Single();

            Assert.Equal(2, method.GetParameters().Length);
        }

        [Fact]
        public void IClashRunComparer_FirstParameterIsNamedPreviousRun()
        {
            var method = typeof(IClashRunComparer).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Single();

            Assert.Equal("previousRun", method.GetParameters()[0].Name);
        }

        [Fact]
        public void IClashRunComparer_SecondParameterIsNamedCurrentRun()
        {
            var method = typeof(IClashRunComparer).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Single();

            Assert.Equal("currentRun", method.GetParameters()[1].Name);
        }

        [Fact]
        public void IClashRunComparer_BothParametersAreCoordinationRun()
        {
            var method = typeof(IClashRunComparer).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Single();
            var parameters = method.GetParameters();

            Assert.Equal(typeof(CoordinationRun), parameters[0].ParameterType);
            Assert.Equal(typeof(CoordinationRun), parameters[1].ParameterType);
        }

        [Fact]
        public void IClashRunComparer_DoesNotAcceptACollection()
        {
            var method = typeof(IClashRunComparer).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Single();

            Assert.DoesNotContain(method.GetParameters(), p => typeof(System.Collections.IEnumerable).IsAssignableFrom(p.ParameterType));
        }

        [Fact]
        public void IClashRunComparer_IsNotAsync()
        {
            var method = typeof(IClashRunComparer).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Single();

            Assert.False(typeof(System.Threading.Tasks.Task).IsAssignableFrom(method.ReturnType));
        }

        [Fact]
        public void IClashRunComparer_DoesNotAcceptCancellationToken()
        {
            var method = typeof(IClashRunComparer).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Single();

            Assert.DoesNotContain(method.GetParameters(), p => p.ParameterType == typeof(CancellationToken));
        }

        // --- Implementability ---

        [Fact]
        public void IClashRunComparer_IsImplementableOutsideCore_ByDelegation()
        {
            IClashRunComparer fake = new DelegatingClashRunComparer(new DeterministicClashRunComparer(new NullClashMatcher()));

            var result = fake.Compare(MakeEmptyRun("run-1"), MakeEmptyRun("run-2"));

            Assert.NotNull(result);
            Assert.Empty(result.Candidates);
        }
    }
}
