using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using OrzioClashReport.Core.Abstractions;
using OrzioClashReport.Core.Lifecycle;
using OrzioClashReport.Core.Matching;
using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Tests
{
    /// <summary>Locks the public shape of IClashLifecycleClassifier and proves it is implementable end to end.</summary>
    public class IClashLifecycleClassifierTests
    {
        private static ModelRevision MakeRevision(string company, string discipline, string modelName, string revision) =>
            new ModelRevision(new ModelIdentity(company, discipline, modelName), revision, $"{modelName}_{revision}.nwc", null, null, null);

        private static RunManifest MakeManifest(string runId, params ModelRevision[] models) =>
            new RunManifest(runId, new DateTimeOffset(2026, 7, 10, 9, 0, 0, TimeSpan.Zero), models, Array.Empty<ExecutedClashTest>());

        private static CoordinationRun MakeEmptyRun(string runId) =>
            new CoordinationRun(MakeManifest(runId, MakeRevision("Sigma", "Structure", "Main", "R04")), Array.Empty<ClashOccurrence>());

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

        /// <summary>Test-only fake: a private type implementing IClashLifecycleClassifier by delegating to a real classifier, proving the interface is implementable outside the Core assembly's internals.</summary>
        private sealed class DelegatingClashLifecycleClassifier : IClashLifecycleClassifier
        {
            private readonly IClashLifecycleClassifier _inner;

            public DelegatingClashLifecycleClassifier(IClashLifecycleClassifier inner)
            {
                _inner = inner;
            }

            public ClashLifecycleResult Classify(ClashRunMatchResult matchResult) => _inner.Classify(matchResult);
        }

        // --- Public shape, via reflection ---

        [Fact]
        public void IClashLifecycleClassifier_IsPublicInterface()
        {
            var type = typeof(IClashLifecycleClassifier);

            Assert.True(type.IsInterface);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void IClashLifecycleClassifier_DeclaresExactlyOnePublicMethod()
        {
            var methods = typeof(IClashLifecycleClassifier).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            Assert.Single(methods);
        }

        [Fact]
        public void IClashLifecycleClassifier_MethodIsNamedClassify()
        {
            var method = typeof(IClashLifecycleClassifier).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Single();

            Assert.Equal("Classify", method.Name);
        }

        [Fact]
        public void IClashLifecycleClassifier_ReturnTypeIsClashLifecycleResult()
        {
            var method = typeof(IClashLifecycleClassifier).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Single();

            Assert.Equal(typeof(ClashLifecycleResult), method.ReturnType);
        }

        [Fact]
        public void IClashLifecycleClassifier_HasExactlyOneParameter()
        {
            var method = typeof(IClashLifecycleClassifier).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Single();

            Assert.Single(method.GetParameters());
        }

        [Fact]
        public void IClashLifecycleClassifier_ParameterIsNamedMatchResult()
        {
            var method = typeof(IClashLifecycleClassifier).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Single();

            Assert.Equal("matchResult", method.GetParameters()[0].Name);
        }

        [Fact]
        public void IClashLifecycleClassifier_ParameterIsClashRunMatchResult()
        {
            var method = typeof(IClashLifecycleClassifier).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Single();

            Assert.Equal(typeof(ClashRunMatchResult), method.GetParameters()[0].ParameterType);
        }

        [Fact]
        public void IClashLifecycleClassifier_DoesNotAcceptACollection()
        {
            var method = typeof(IClashLifecycleClassifier).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Single();

            Assert.DoesNotContain(method.GetParameters(), p => typeof(System.Collections.IEnumerable).IsAssignableFrom(p.ParameterType));
        }

        [Fact]
        public void IClashLifecycleClassifier_IsNotAsync()
        {
            var method = typeof(IClashLifecycleClassifier).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Single();

            Assert.False(typeof(System.Threading.Tasks.Task).IsAssignableFrom(method.ReturnType));
        }

        [Fact]
        public void IClashLifecycleClassifier_DoesNotAcceptCancellationToken()
        {
            var method = typeof(IClashLifecycleClassifier).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Single();

            Assert.DoesNotContain(method.GetParameters(), p => p.ParameterType == typeof(CancellationToken));
        }

        // --- Implementability ---

        [Fact]
        public void IClashLifecycleClassifier_IsImplementableOutsideCore_ByDelegation()
        {
            IClashLifecycleClassifier fake = new DelegatingClashLifecycleClassifier(new ConservativeClashLifecycleClassifier());
            var matchResult = new DeterministicClashRunComparer(new NullClashMatcher()).Compare(MakeEmptyRun("run-1"), MakeEmptyRun("run-2"));

            var result = fake.Classify(matchResult);

            Assert.NotNull(result);
            Assert.Empty(result.Entries);
        }
    }
}
