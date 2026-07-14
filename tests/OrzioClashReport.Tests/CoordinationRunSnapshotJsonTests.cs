using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using OrzioClashReport.Core.Model;
using OrzioClashReport.Persistence.RunSnapshotJson;

namespace OrzioClashReport.Tests
{
    /// <summary>
    /// Proves the immutable schema-v1 coordination run snapshot: deterministic serialization, exact-instance
    /// rehydration by model index, strict raw evidence (raw ClashStatus, canonicalized Properties), strict JSON
    /// contract enforcement, and create-new immutability on Save. No matching or lifecycle data is persisted.
    /// </summary>
    public sealed class CoordinationRunSnapshotJsonTests
    {
        private static JsonCoordinationRunSnapshotSerializer Sut() => new JsonCoordinationRunSnapshotSerializer();

        // ===============================================================================================
        //  Serializer basics
        // ===============================================================================================

        [Fact]
        public void Serialize_NullRun_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => Sut().Serialize(null!));
        }

        [Fact]
        public void Parse_NullJson_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => Sut().Parse(null!));
        }

        [Fact]
        public void Parse_WhitespaceJson_ThrowsFormatException()
        {
            var ex = Assert.Throws<CoordinationRunSnapshotFormatException>(() => Sut().Parse("   \n\t "));
            Assert.Contains("cannot be empty or whitespace", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void Save_NullRun_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => Sut().Save(null!, "whatever.json"));
        }

        [Fact]
        public void Save_NullPath_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => Sut().Save(BuildFullFixtureRun(), null!));
        }

        [Fact]
        public void Save_WhitespacePath_ThrowsFormatException()
        {
            var ex = Assert.Throws<CoordinationRunSnapshotFormatException>(() => Sut().Save(BuildFullFixtureRun(), "   "));
            Assert.Contains("file path cannot be empty or whitespace", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void Load_NullPath_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => Sut().Load(null!));
        }

        [Fact]
        public void Load_WhitespacePath_ThrowsFormatException()
        {
            Assert.Throws<CoordinationRunSnapshotFormatException>(() => Sut().Load("   "));
        }

        [Fact]
        public void Load_MissingFile_ThrowsFormatException()
        {
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".json");
            var ex = Assert.Throws<CoordinationRunSnapshotFormatException>(() => Sut().Load(path));
            // Assert the adapter's own context, not an OS-specific message.
            Assert.Contains("Coordination run snapshot file not found:", ex.Message, StringComparison.Ordinal);
        }

        // ===============================================================================================
        //  Determinism and golden
        // ===============================================================================================

        [Fact]
        public void Serialize_ProducesByteIdenticalOutputOnRepeatedRuns()
        {
            var run = BuildFullFixtureRun();
            var sut = Sut();

            string first = sut.Serialize(run);
            string second = sut.Serialize(run);

            Assert.Equal(first, second, StringComparer.Ordinal);
            Assert.EndsWith("\n", first, StringComparison.Ordinal);
            Assert.False(first.EndsWith("\n\n", StringComparison.Ordinal), "snapshot must end with exactly one newline");
        }

        [Fact]
        public void Serialize_MatchesCoordinationRunSnapshotGoldenFile()
        {
            var run = BuildFullFixtureRun();
            string actual = Sut().Serialize(run);

            string goldenPath = Path.Combine(AppContext.BaseDirectory, "Golden", "coordination-run-snapshot.golden.json");
            string expected = File.ReadAllText(goldenPath);

            Assert.Equal(expected, actual, StringComparer.Ordinal);
        }

        // ===============================================================================================
        //  Full round-trip
        // ===============================================================================================

        [Fact]
        public void Parse_SerializeRoundTrip_PreservesCompleteRunEvidence()
        {
            var original = BuildFullFixtureRun();
            var sut = Sut();

            CoordinationRun loaded = sut.Parse(sut.Serialize(original));

            // Run-level facts.
            Assert.Equal("coordination-run-001", loaded.RunId);
            Assert.Equal(original.CreatedAt, loaded.CreatedAt);
            Assert.Equal(2, loaded.Models.Count);
            Assert.Equal(2, loaded.ExecutedClashTests.Count);
            Assert.Equal(4, loaded.Occurrences.Count);

            // Models, by index (including all optional metadata).
            ModelRevision structure = loaded.Models[0];
            Assert.Equal("Sigma", structure.Identity.Company);
            Assert.Equal("Structure", structure.Identity.Discipline);
            Assert.Equal("Main", structure.Identity.ModelName);
            Assert.Equal("R04", structure.Revision);
            Assert.Equal("Sigma_Main_R04.nwc", structure.SourceFileName);
            Assert.Null(structure.SourceFilePath);
            Assert.Null(structure.ContentHash);
            Assert.Null(structure.PublishedAt);

            ModelRevision hvac = loaded.Models[1];
            Assert.Equal("Alfa", hvac.Identity.Company);
            Assert.Equal("HVAC", hvac.Identity.Discipline);
            Assert.Equal("Coordination", hvac.Identity.ModelName);
            Assert.Equal("R07", hvac.Revision);
            Assert.Equal("Alfa_Coordination_R07.nwc", hvac.SourceFileName);
            Assert.Equal("C:\\coord\\Alfa_Coordination_R07.nwc", hvac.SourceFilePath);
            Assert.Equal("sha256:abcdef0123456789", hvac.ContentHash);
            Assert.Equal(new DateTimeOffset(2026, 7, 10, 8, 30, 0, TimeSpan.FromHours(1)), hvac.PublishedAt);

            // Executed tests, by index, including exact identity instance reuse.
            ExecutedClashTest twoModel = loaded.ExecutedClashTests[0];
            Assert.Equal("HVAC vs Structure", twoModel.Name);
            Assert.True(twoModel.ModelA.Equals(structure.Identity));
            Assert.True(twoModel.ModelB.Equals(hvac.Identity));
            Assert.Same(loaded.Models[0].Identity, twoModel.ModelA);
            Assert.Same(loaded.Models[1].Identity, twoModel.ModelB);

            ExecutedClashTest selfTest = loaded.ExecutedClashTests[1];
            Assert.Equal("Structure self clash", selfTest.Name);
            Assert.Same(loaded.Models[0].Identity, selfTest.ModelA);
            Assert.Same(loaded.Models[0].Identity, selfTest.ModelB);

            // Occurrence slots: values, A/B orientation, and exact revision instance reuse.
            AssertOccurrenceModelInstances(loaded, 0, 0, 1);
            AssertOccurrenceModelInstances(loaded, 1, 0, 1); // duplicate slot preserved
            AssertOccurrenceModelInstances(loaded, 2, 0, 0); // self-clash
            AssertOccurrenceModelInstances(loaded, 3, 1, 0); // swapped A/B orientation

            // Slot 0 (and its duplicate slot 1) full evidence.
            ClashOccurrence slot0 = loaded.Occurrences[0];
            Assert.Equal("HVAC vs Structure", slot0.ClashTestName);
            Assert.Equal("Clash 001", slot0.Clash.Name);
            Assert.Equal(ClashStatus.Active, slot0.Clash.Status);
            Assert.Equal(-0.125, slot0.Clash.Distance);
            Assert.Equal("A-01", slot0.Clash.GridLocation);
            Assert.NotNull(slot0.Clash.Point);
            Assert.Equal(1.25, slot0.Clash.Point!.Value.X);
            Assert.Equal(2.5, slot0.Clash.Point!.Value.Y);
            Assert.Equal(3.75, slot0.Clash.Point!.Value.Z);
            Assert.Equal("guid-001", slot0.Clash.Guid);
            Assert.Equal("s-100", slot0.Clash.ElementA.ElementId);
            Assert.Equal("Beam 100", slot0.Clash.ElementA.ElementName);
            Assert.Equal("L01", slot0.Clash.ElementA.Level);
            Assert.Equal("Sigma_Main_R04.nwc", slot0.Clash.ElementA.SourceModel);
            Assert.Equal(new[] { "Project", "Structure", "Beam 100" }, slot0.Clash.ElementA.PathHierarchy);
            Assert.Equal(3, slot0.Clash.ElementA.Properties.Count);
            Assert.Equal("Structural Framing", slot0.Clash.ElementA.Properties["Category"]);
            Assert.Equal("s-100", slot0.Clash.ElementA.Properties["Element Id"]);
            Assert.Equal("Sigma_Main_R04.nwc", slot0.Clash.ElementA.Properties["Item Source File"]);
            Assert.Empty(slot0.Clash.ElementB.Properties);

            // The duplicate slot carries value-equal evidence. JSON persists values/slots, not the shared
            // ClashResult object graph, so we do not require ReferenceEquals between slot 0 and slot 1 clashes.
            ClashOccurrence slot1 = loaded.Occurrences[1];
            Assert.Equal(slot0.Clash.Name, slot1.Clash.Name);
            Assert.Equal(slot0.Clash.ElementA.ElementId, slot1.Clash.ElementA.ElementId);

            // Slot 2: self-clash with all-null clash scalars and empty collections.
            ClashOccurrence slot2 = loaded.Occurrences[2];
            Assert.Equal("Structure self clash", slot2.ClashTestName);
            Assert.Null(slot2.Clash.Name);
            Assert.Equal(ClashStatus.Resolved, slot2.Clash.Status);
            Assert.Null(slot2.Clash.Distance);
            Assert.Null(slot2.Clash.GridLocation);
            Assert.Null(slot2.Clash.Point);
            Assert.Null(slot2.Clash.Guid);
            Assert.Empty(slot2.Clash.ElementA.PathHierarchy);
            Assert.Empty(slot2.Clash.ElementA.Properties);
            Assert.Null(slot2.Clash.ElementA.ElementName);
            Assert.Null(slot2.Clash.ElementA.Level);
            Assert.Null(slot2.Clash.ElementA.SourceModel);

            // Slot 3: swapped orientation, Unknown raw status.
            ClashOccurrence slot3 = loaded.Occurrences[3];
            Assert.Equal(ClashStatus.Unknown, slot3.Clash.Status);
            Assert.Equal(12.0, slot3.Clash.Distance);
            Assert.Equal("h-400", slot3.Clash.ElementA.ElementId);
            Assert.Equal("s-401", slot3.Clash.ElementB.ElementId);
        }

        private static void AssertOccurrenceModelInstances(CoordinationRun run, int slot, int expectedAIndex, int expectedBIndex)
        {
            Assert.Same(run.Models[expectedAIndex], run.Occurrences[slot].ModelA);
            Assert.Same(run.Models[expectedBIndex], run.Occurrences[slot].ModelB);
        }

        // ===============================================================================================
        //  Canonical Properties ordering and PathHierarchy ordering
        // ===============================================================================================

        [Fact]
        public void Serialize_Properties_AreOrderedByOrdinalKey()
        {
            var properties = new Dictionary<string, string>();
            properties["b"] = "vb";
            properties["a"] = "va";
            properties["A"] = "vA";

            var elementA = new ClashObject("s-1", null, null, null, new List<string>(), properties);
            CoordinationRun run = RunWithSingleClash(SimpleClash(elementA, SimpleObject("h-1")));

            string json = Sut().Serialize(run);

            int iUpperA = json.IndexOf("\"key\": \"A\"", StringComparison.Ordinal);
            int iLowerA = json.IndexOf("\"key\": \"a\"", StringComparison.Ordinal);
            int iLowerB = json.IndexOf("\"key\": \"b\"", StringComparison.Ordinal);

            Assert.True(iUpperA >= 0 && iLowerA >= 0 && iLowerB >= 0);
            // Ordinal order: 'A' (65) < 'a' (97) < 'b' (98).
            Assert.True(iUpperA < iLowerA, "'A' must precede 'a' under ordinal ordering");
            Assert.True(iLowerA < iLowerB, "'a' must precede 'b' under ordinal ordering");
        }

        [Fact]
        public void Parse_SerializeRoundTrip_PreservesPathHierarchyOrder()
        {
            var path = new List<string> { "Root", "Nested Model", "Discipline", "Element" };
            var elementA = new ClashObject("s-1", null, null, null, path, new Dictionary<string, string>());
            CoordinationRun run = RunWithSingleClash(SimpleClash(elementA, SimpleObject("h-1")));

            var sut = Sut();
            CoordinationRun loaded = sut.Parse(sut.Serialize(run));

            // Assert.Equal on sequences is order-sensitive (SequenceEqual semantics).
            Assert.Equal(path, loaded.Occurrences[0].Clash.ElementA.PathHierarchy);
        }

        // ===============================================================================================
        //  Schema strictness
        // ===============================================================================================

        [Fact]
        public void Parse_MissingSchemaVersion_ThrowsFormatException()
        {
            string json = """
            { "runId": "r", "createdAt": "2026-07-14T09:00:00Z", "models": [], "executedClashTests": [], "occurrences": [] }
            """;
            var ex = Assert.Throws<CoordinationRunSnapshotFormatException>(() => Sut().Parse(json));
            Assert.Contains("'schemaVersion' is required", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void Parse_UnsupportedSchemaVersion_ThrowsFormatException()
        {
            var ex = Assert.Throws<CoordinationRunSnapshotFormatException>(() => Sut().Parse(Doc(schema: "2")));
            Assert.Contains("Unsupported coordination run snapshot schemaVersion '2'", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void Parse_UnknownRootProperty_ThrowsFormatException()
        {
            string json = Doc().Replace("\"schemaVersion\": 1,", "\"schemaVersion\": 1,\n  \"unknownRoot\": true,", StringComparison.Ordinal);
            Assert.Throws<CoordinationRunSnapshotFormatException>(() => Sut().Parse(json));
        }

        [Fact]
        public void Parse_UnknownNestedProperty_ThrowsFormatException()
        {
            string json = Doc().Replace("\"status\": \"Active\",", "\"status\": \"Active\", \"unknownNested\": 1,", StringComparison.Ordinal);
            Assert.Throws<CoordinationRunSnapshotFormatException>(() => Sut().Parse(json));
        }

        [Fact]
        public void Parse_DuplicateRootProperty_ThrowsFormatException()
        {
            string json = Doc().Replace("\"runId\": \"run-parse\",", "\"runId\": \"run-parse\",\n  \"runId\": \"again\",", StringComparison.Ordinal);
            var ex = Assert.Throws<CoordinationRunSnapshotFormatException>(() => Sut().Parse(json));
            Assert.Contains("Duplicate JSON property 'runId' at root", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void Parse_DuplicateDeepProperty_ThrowsFormatException()
        {
            string json = Doc().Replace("\"elementId\": \"s-100\",", "\"elementId\": \"s-100\", \"elementId\": \"dup\",", StringComparison.Ordinal);
            var ex = Assert.Throws<CoordinationRunSnapshotFormatException>(() => Sut().Parse(json));
            Assert.Contains("occurrences[0].clash.elementA", ex.Message, StringComparison.Ordinal);
        }

        // ===============================================================================================
        //  Required collections
        // ===============================================================================================

        [Fact]
        public void Parse_MissingModels_ThrowsFormatException()
        {
            string json = """
            { "schemaVersion": 1, "runId": "r", "createdAt": "2026-07-14T09:00:00Z", "executedClashTests": [], "occurrences": [] }
            """;
            var ex = Assert.Throws<CoordinationRunSnapshotFormatException>(() => Sut().Parse(json));
            Assert.Contains("'models' is required", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void Parse_MissingExecutedClashTests_ThrowsFormatException()
        {
            string json = $$"""
            { "schemaVersion": 1, "runId": "r", "createdAt": "2026-07-14T09:00:00Z", "models": [ {{ModelSigma}} ], "occurrences": [] }
            """;
            var ex = Assert.Throws<CoordinationRunSnapshotFormatException>(() => Sut().Parse(json));
            Assert.Contains("'executedClashTests' is required", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void Parse_MissingOccurrences_ThrowsFormatException()
        {
            string json = $$"""
            { "schemaVersion": 1, "runId": "r", "createdAt": "2026-07-14T09:00:00Z", "models": [ {{ModelSigma}} ], "executedClashTests": [] }
            """;
            var ex = Assert.Throws<CoordinationRunSnapshotFormatException>(() => Sut().Parse(json));
            Assert.Contains("'occurrences' is required", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void Parse_EmptyModels_ThrowsFormatExceptionThroughCoreValidation()
        {
            string json = """
            { "schemaVersion": 1, "runId": "r", "createdAt": "2026-07-14T09:00:00Z", "models": [], "executedClashTests": [], "occurrences": [] }
            """;
            var ex = Assert.Throws<CoordinationRunSnapshotFormatException>(() => Sut().Parse(json));
            Assert.Contains("Coordination run snapshot is invalid", ex.Message, StringComparison.Ordinal);
            Assert.IsAssignableFrom<ArgumentException>(ex.InnerException);
        }

        [Fact]
        public void Parse_EmptyExecutedClashTests_IsAllowedWhenOccurrencesAreEmpty()
        {
            string json = $$"""
            { "schemaVersion": 1, "runId": "r", "createdAt": "2026-07-14T09:00:00Z", "models": [ {{ModelSigma}} ], "executedClashTests": [], "occurrences": [] }
            """;
            CoordinationRun run = Sut().Parse(json);
            Assert.Empty(run.ExecutedClashTests);
            Assert.Empty(run.Occurrences);
            Assert.Single(run.Models);
        }

        [Fact]
        public void Parse_EmptyOccurrences_IsAllowed()
        {
            string json = $$"""
            { "schemaVersion": 1, "runId": "r", "createdAt": "2026-07-14T09:00:00Z",
              "models": [ {{ModelSigma}} ],
              "executedClashTests": [ { "name": "Self", "modelAIndex": 0, "modelBIndex": 0 } ],
              "occurrences": [] }
            """;
            CoordinationRun run = Sut().Parse(json);
            Assert.Single(run.ExecutedClashTests);
            Assert.Empty(run.Occurrences);
        }

        // ===============================================================================================
        //  Strict timestamps
        // ===============================================================================================

        [Fact]
        public void Parse_CreatedAtWithoutOffset_ThrowsFormatException()
        {
            Assert.Throws<CoordinationRunSnapshotFormatException>(
                () => Sut().Parse(Doc(created: "\"2026-07-14T09:00:00\"")));
        }

        [Fact]
        public void Parse_PublishedAtWithoutOffset_ThrowsFormatException()
        {
            Assert.Throws<CoordinationRunSnapshotFormatException>(
                () => Sut().Parse(Doc(published: "\"2026-07-14T09:00:00\"")));
        }

        [Fact]
        public void Parse_CreatedAtWithZ_IsAccepted()
        {
            CoordinationRun run = Sut().Parse(Doc(created: "\"2026-07-14T08:00:00Z\""));
            Assert.Equal(new DateTimeOffset(2026, 7, 14, 8, 0, 0, TimeSpan.Zero), run.CreatedAt);
        }

        [Fact]
        public void Parse_PublishedAtWithExplicitOffset_IsAccepted()
        {
            CoordinationRun run = Sut().Parse(Doc(published: "\"2026-07-10T08:30:00+01:00\""));
            Assert.Equal(new DateTimeOffset(2026, 7, 10, 8, 30, 0, TimeSpan.FromHours(1)), run.Models[0].PublishedAt);
        }

        // ===============================================================================================
        //  Strict raw status
        // ===============================================================================================

        [Fact]
        public void Parse_ExactClashStatusNames_AreAccepted()
        {
            var expected = new (string Name, ClashStatus Status)[]
            {
                ("New", ClashStatus.New),
                ("Active", ClashStatus.Active),
                ("Reviewed", ClashStatus.Reviewed),
                ("Approved", ClashStatus.Approved),
                ("Resolved", ClashStatus.Resolved),
                ("Unknown", ClashStatus.Unknown),
            };

            foreach (var (name, status) in expected)
            {
                CoordinationRun run = Sut().Parse(Doc(status: $"\"{name}\""));
                Assert.Equal(status, run.Occurrences[0].Clash.Status);
            }
        }

        [Fact]
        public void Parse_LowercaseClashStatus_ThrowsFormatException()
        {
            Assert.Throws<CoordinationRunSnapshotFormatException>(() => Sut().Parse(Doc(status: "\"active\"")));
        }

        [Fact]
        public void Parse_NumericClashStatus_ThrowsFormatException()
        {
            Assert.Throws<CoordinationRunSnapshotFormatException>(() => Sut().Parse(Doc(status: "1")));
        }

        [Fact]
        public void Parse_LifecycleStatusName_ThrowsFormatException()
        {
            Assert.Throws<CoordinationRunSnapshotFormatException>(() => Sut().Parse(Doc(status: "\"StillOpen\"")));
        }

        // ===============================================================================================
        //  Index validation
        // ===============================================================================================

        [Fact]
        public void Parse_MissingOccurrenceModelIndex_ThrowsFormatException()
        {
            var ex = Assert.Throws<CoordinationRunSnapshotFormatException>(() => Sut().Parse(Doc(occAProp: "")));
            Assert.Contains("'occurrences[0].modelAIndex' is required", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void Parse_NegativeOccurrenceModelIndex_ThrowsFormatException()
        {
            var ex = Assert.Throws<CoordinationRunSnapshotFormatException>(() => Sut().Parse(Doc(occBProp: "\"modelBIndex\": -1,")));
            Assert.Contains("'occurrences[0].modelBIndex' cannot be negative", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void Parse_OutOfRangeOccurrenceModelIndex_ThrowsFormatException()
        {
            var ex = Assert.Throws<CoordinationRunSnapshotFormatException>(() => Sut().Parse(Doc(occAProp: "\"modelAIndex\": 7,")));
            Assert.Contains("references model index 7, but the snapshot declares 2 models", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void Parse_MissingExecutedTestModelIndex_ThrowsFormatException()
        {
            var ex = Assert.Throws<CoordinationRunSnapshotFormatException>(() => Sut().Parse(Doc(testBProp: "")));
            Assert.Contains("'executedClashTests[0].modelBIndex' is required", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void Parse_NegativeExecutedTestModelIndex_ThrowsFormatException()
        {
            var ex = Assert.Throws<CoordinationRunSnapshotFormatException>(() => Sut().Parse(Doc(testAProp: "\"modelAIndex\": -1,")));
            Assert.Contains("'executedClashTests[0].modelAIndex' cannot be negative", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void Parse_OutOfRangeExecutedTestModelIndex_ThrowsFormatException()
        {
            var ex = Assert.Throws<CoordinationRunSnapshotFormatException>(() => Sut().Parse(Doc(testBProp: "\"modelBIndex\": 7,")));
            Assert.Contains("'executedClashTests[0].modelBIndex' references model index 7", ex.Message, StringComparison.Ordinal);
        }

        // ===============================================================================================
        //  Property entry validation
        // ===============================================================================================

        [Fact]
        public void Parse_DuplicatePropertyEntryKey_ThrowsFormatException()
        {
            string props = "[ { \"key\": \"K\", \"value\": \"1\" }, { \"key\": \"K\", \"value\": \"2\" } ]";
            var ex = Assert.Throws<CoordinationRunSnapshotFormatException>(() => Sut().Parse(Doc(propsA: props)));
            Assert.Contains("Occurrence at index 0 elementA contains duplicate property key 'K'", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void Parse_CaseDistinctPropertyEntryKeys_AreAccepted()
        {
            string props = "[ { \"key\": \"Tag\", \"value\": \"upper\" }, { \"key\": \"tag\", \"value\": \"lower\" } ]";
            CoordinationRun run = Sut().Parse(Doc(propsA: props));

            var properties = run.Occurrences[0].Clash.ElementA.Properties;
            Assert.Equal("upper", properties["Tag"]);
            Assert.Equal("lower", properties["tag"]);
        }

        [Fact]
        public void Parse_MissingPropertyEntryKey_ThrowsFormatException()
        {
            var ex = Assert.Throws<CoordinationRunSnapshotFormatException>(() => Sut().Parse(Doc(propsA: "[ { \"value\": \"1\" } ]")));
            Assert.Contains(".key' is required", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void Parse_MissingPropertyEntryValue_ThrowsFormatException()
        {
            var ex = Assert.Throws<CoordinationRunSnapshotFormatException>(() => Sut().Parse(Doc(propsA: "[ { \"key\": \"K\" } ]")));
            Assert.Contains(".value' is required", ex.Message, StringComparison.Ordinal);
        }

        // ===============================================================================================
        //  Exact model instance reuse on Parse
        // ===============================================================================================

        [Fact]
        public void Parse_OccurrenceModelIndexesReuseExactManifestModelInstances()
        {
            var sut = Sut();
            CoordinationRun loaded = sut.Parse(sut.Serialize(BuildFullFixtureRun()));

            // The rehydrated occurrences must reference the exact ModelRevision instances addressed by their
            // indexes, not merely value-equal copies.
            Assert.Same(loaded.Models[0], loaded.Occurrences[0].ModelA);
            Assert.Same(loaded.Models[1], loaded.Occurrences[0].ModelB);
            Assert.Same(loaded.Models[0], loaded.Occurrences[2].ModelA);
            Assert.Same(loaded.Models[0], loaded.Occurrences[2].ModelB); // self-clash
            Assert.Same(loaded.Models[1], loaded.Occurrences[3].ModelA); // swapped orientation
            Assert.Same(loaded.Models[0], loaded.Occurrences[3].ModelB);
        }

        [Fact]
        public void Parse_ExecutedTestModelIndexesReuseExactManifestIdentityInstances()
        {
            var sut = Sut();
            CoordinationRun loaded = sut.Parse(sut.Serialize(BuildFullFixtureRun()));

            Assert.Same(loaded.Models[0].Identity, loaded.ExecutedClashTests[0].ModelA);
            Assert.Same(loaded.Models[1].Identity, loaded.ExecutedClashTests[0].ModelB);
            Assert.Same(loaded.Models[0].Identity, loaded.ExecutedClashTests[1].ModelA);
            Assert.Same(loaded.Models[0].Identity, loaded.ExecutedClashTests[1].ModelB);
        }

        // ===============================================================================================
        //  Save immutability
        // ===============================================================================================

        [Fact]
        public void Save_CreatesUtf8WithoutBomFileMatchingSerializeExactly()
        {
            var run = BuildFullFixtureRun();
            var sut = Sut();
            string dir = NewTempDir();
            try
            {
                string path = Path.Combine(dir, "snapshot.json");
                sut.Save(run, path);

                byte[] bytes = File.ReadAllBytes(path);
                Assert.False(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF,
                    "snapshot file must not start with a UTF-8 BOM");

                string decoded = new UTF8Encoding(false).GetString(bytes);
                Assert.Equal(sut.Serialize(run), decoded, StringComparer.Ordinal);
            }
            finally
            {
                Directory.Delete(dir, recursive: true);
            }
        }

        [Fact]
        public void Save_ExistingFile_RefusesOverwriteAndPreservesOriginalBytes()
        {
            var run = BuildFullFixtureRun();
            var sut = Sut();
            string dir = NewTempDir();
            try
            {
                string path = Path.Combine(dir, "snapshot.json");
                byte[] sentinel = Encoding.UTF8.GetBytes("ORIGINAL-SNAPSHOT-SENTINEL");
                File.WriteAllBytes(path, sentinel);

                var ex = Assert.Throws<CoordinationRunSnapshotFormatException>(() => sut.Save(run, path));
                Assert.Contains("Coordination run snapshot file already exists:", ex.Message, StringComparison.Ordinal);

                Assert.Equal(sentinel, File.ReadAllBytes(path));
            }
            finally
            {
                Directory.Delete(dir, recursive: true);
            }
        }

        [Fact]
        public void Save_SerializationFailure_DoesNotCreateDestinationFile()
        {
            // double.NaN is a valid Core value but is not representable by the strict JSON serializer, so
            // serialization fails before any file is created. (Verified: System.Text.Json throws for NaN under
            // the default number handling this adapter uses.)
            var clash = new ClashResult(
                "NaN clash", ClashStatus.Active, double.NaN, null, null,
                SimpleObject("s-1"), SimpleObject("h-1"), null);
            CoordinationRun run = RunWithSingleClash(clash);

            var sut = Sut();
            string dir = NewTempDir();
            try
            {
                string path = Path.Combine(dir, "snapshot.json");

                Exception? ex = Record.Exception(() => sut.Save(run, path));

                Assert.NotNull(ex);
                Assert.False(File.Exists(path), "no destination file must exist after a serialization failure");
            }
            finally
            {
                Directory.Delete(dir, recursive: true);
            }
        }

        [Fact]
        public void Save_SameRunIdToDifferentNewPaths_IsAllowed()
        {
            var run = BuildFullFixtureRun();
            var sut = Sut();
            string dir = NewTempDir();
            try
            {
                string pathA = Path.Combine(dir, "a.json");
                string pathB = Path.Combine(dir, "b.json");

                sut.Save(run, pathA);
                sut.Save(run, pathB);

                Assert.Equal(File.ReadAllBytes(pathA), File.ReadAllBytes(pathB));
            }
            finally
            {
                Directory.Delete(dir, recursive: true);
            }
        }

        // ===============================================================================================
        //  No derived comparison/lifecycle fields
        // ===============================================================================================

        [Fact]
        public void Serialize_DoesNotPersistDerivedComparisonOrLifecycleFields()
        {
            string json = Sut().Serialize(BuildFullFixtureRun());

            string[] forbidden =
            {
                "\"candidates\"", "\"selectedMatches\"", "\"alternativeCandidates\"", "\"confidence\"",
                "\"matchEvidence\"", "\"lifecycleEvidence\"", "\"lifecycleEntries\"", "\"lifecycleStatus\"",
                "\"stableClashId\"", "\"persistentClashId\"", "\"fingerprint\"",
            };

            foreach (string token in forbidden)
            {
                Assert.DoesNotContain(token, json, StringComparison.Ordinal);
            }

            // Raw ClashStatus.Resolved is valid persisted evidence and must remain present.
            Assert.Contains("\"status\": \"Resolved\"", json, StringComparison.Ordinal);
        }

        // ===============================================================================================
        //  Fixtures and JSON builders
        // ===============================================================================================

        private const string ModelSigma =
            "{ \"company\": \"Sigma\", \"discipline\": \"Structure\", \"modelName\": \"Main\", \"revision\": \"R04\", "
            + "\"sourceFileName\": \"a.nwc\", \"sourceFilePath\": null, \"contentHash\": null, \"publishedAt\": null }";

        private const string DocTemplate = """
        {
          "schemaVersion": SCHEMA,
          "runId": "run-parse",
          "createdAt": CREATED,
          "models": [
            { "company": "Sigma", "discipline": "Structure", "modelName": "Main", "revision": "R04", "sourceFileName": "a.nwc", "sourceFilePath": null, "contentHash": null, "publishedAt": PUBLISHED },
            { "company": "Alfa", "discipline": "HVAC", "modelName": "Coordination", "revision": "R07", "sourceFileName": "b.nwc", "sourceFilePath": null, "contentHash": null, "publishedAt": null }
          ],
          "executedClashTests": [
            {
              TESTA_PROP
              TESTB_PROP
              "name": "T"
            }
          ],
          "occurrences": [
            {
              OCCA_PROP
              OCCB_PROP
              "clashTestName": "T",
              "clash": {
                "name": "Clash 001",
                "status": STATUS,
                "distance": -0.125,
                "gridLocation": "A-01",
                "point": { "x": 1.25, "y": 2.5, "z": 3.75 },
                "elementA": { "elementId": "s-100", "elementName": "Beam", "level": "L01", "sourceModel": "a.nwc", "pathHierarchy": ["Project", "Structure"], "properties": PROPSA },
                "elementB": { "elementId": "h-200", "elementName": null, "level": null, "sourceModel": null, "pathHierarchy": [], "properties": [] },
                "guid": "guid-001"
              }
            }
          ]
        }
        """;

        /// <summary>
        /// Builds a valid two-model snapshot JSON with defaults, allowing individual pieces to be swapped for
        /// negative tests. Property-line placeholders (e.g. <c>OCCA_PROP</c>) can be set to "" to omit a
        /// property entirely, which keeps the surrounding JSON well-formed so the adapter's own "required"
        /// validation is exercised rather than a raw JSON syntax error.
        /// </summary>
        private static string Doc(
            string schema = "1",
            string created = "\"2026-07-14T09:00:00.0000000+01:00\"",
            string published = "null",
            string testAProp = "\"modelAIndex\": 0,",
            string testBProp = "\"modelBIndex\": 1,",
            string occAProp = "\"modelAIndex\": 0,",
            string occBProp = "\"modelBIndex\": 1,",
            string status = "\"Active\"",
            string propsA = "[]")
        {
            return DocTemplate
                .Replace("SCHEMA", schema, StringComparison.Ordinal)
                .Replace("CREATED", created, StringComparison.Ordinal)
                .Replace("PUBLISHED", published, StringComparison.Ordinal)
                .Replace("TESTA_PROP", testAProp, StringComparison.Ordinal)
                .Replace("TESTB_PROP", testBProp, StringComparison.Ordinal)
                .Replace("OCCA_PROP", occAProp, StringComparison.Ordinal)
                .Replace("OCCB_PROP", occBProp, StringComparison.Ordinal)
                .Replace("STATUS", status, StringComparison.Ordinal)
                .Replace("PROPSA", propsA, StringComparison.Ordinal);
        }

        private static ClashObject SimpleObject(string elementId) =>
            new ClashObject(elementId, null, null, null, new List<string>(), new Dictionary<string, string>());

        private static ClashResult SimpleClash(ClashObject elementA, ClashObject elementB) =>
            new ClashResult("Clash", ClashStatus.Active, null, null, null, elementA, elementB, null);

        private static CoordinationRun RunWithSingleClash(ClashResult clash)
        {
            var structureId = new ModelIdentity("Sigma", "Structure", "Main");
            var hvacId = new ModelIdentity("Alfa", "HVAC", "Coordination");
            var structure = new ModelRevision(structureId, "R04", "a.nwc", null, null, null);
            var hvac = new ModelRevision(hvacId, "R07", "b.nwc", null, null, null);

            var manifest = new RunManifest(
                "run-1",
                new DateTimeOffset(2026, 7, 14, 9, 0, 0, TimeSpan.FromHours(1)),
                new List<ModelRevision> { structure, hvac },
                new List<ExecutedClashTest> { new ExecutedClashTest("T", structureId, hvacId) });

            var occurrence = new ClashOccurrence("T", clash, structure, hvac);
            return new CoordinationRun(manifest, new List<ClashOccurrence> { occurrence });
        }

        /// <summary>
        /// Small synthetic fixture that exercises the full snapshot matrix: full and minimal model metadata,
        /// a two-model test and a self-clash test, four occurrence slots (including a duplicate slot and a
        /// swapped-orientation slot), every optional clash/object field present and absent, empty and non-empty
        /// path hierarchies and properties, deliberately non-ordinal property insertion order, and the raw
        /// statuses Active, Resolved, and Unknown.
        /// </summary>
        private static CoordinationRun BuildFullFixtureRun()
        {
            var structureId = new ModelIdentity("Sigma", "Structure", "Main");
            var hvacId = new ModelIdentity("Alfa", "HVAC", "Coordination");

            var structure = new ModelRevision(structureId, "R04", "Sigma_Main_R04.nwc", null, null, null);
            var hvac = new ModelRevision(
                hvacId, "R07", "Alfa_Coordination_R07.nwc",
                "C:\\coord\\Alfa_Coordination_R07.nwc",
                "sha256:abcdef0123456789",
                new DateTimeOffset(2026, 7, 10, 8, 30, 0, TimeSpan.FromHours(1)));

            var models = new List<ModelRevision> { structure, hvac };

            var tests = new List<ExecutedClashTest>
            {
                new ExecutedClashTest("HVAC vs Structure", structureId, hvacId),
                new ExecutedClashTest("Structure self clash", structureId, structureId),
            };

            var manifest = new RunManifest(
                "coordination-run-001",
                new DateTimeOffset(2026, 7, 14, 9, 0, 0, TimeSpan.FromHours(1)),
                models,
                tests);

            var occ0 = new ClashOccurrence(
                "HVAC vs Structure",
                new ClashResult(
                    "Clash 001", ClashStatus.Active, -0.125, "A-01", new ClashPoint(1.25, 2.5, 3.75),
                    new ClashObject(
                        "s-100", "Beam 100", "L01", "Sigma_Main_R04.nwc",
                        new List<string> { "Project", "Structure", "Beam 100" },
                        NonOrdinalProperties()),
                    new ClashObject(
                        "h-200", "Duct 200", "L01", "Alfa_Coordination_R07.nwc",
                        new List<string> { "Project", "HVAC" },
                        new Dictionary<string, string>()),
                    "guid-001"),
                structure,
                hvac);

            var occ2 = new ClashOccurrence(
                "Structure self clash",
                new ClashResult(
                    null, ClashStatus.Resolved, null, null, null,
                    new ClashObject("s-300", null, null, null, new List<string>(), new Dictionary<string, string>()),
                    new ClashObject(
                        "s-301", "Column 301", "L02", "Sigma_Main_R04.nwc",
                        new List<string> { "Root", "Nested Model", "Discipline", "Element" },
                        new Dictionary<string, string> { { "Level", "L02" }, { "Category", "Structural Columns" } }),
                    null),
                structure,
                structure);

            var occ3 = new ClashOccurrence(
                "HVAC vs Structure",
                new ClashResult(
                    "Clash 004", ClashStatus.Unknown, 12.0, "B-02", new ClashPoint(0.0, 0.0, 0.0),
                    new ClashObject(
                        "h-400", "Pipe 400", "L03", "Alfa_Coordination_R07.nwc",
                        new List<string> { "Project", "HVAC", "Pipe 400" },
                        new Dictionary<string, string> { { "System", "Chilled Water" }, { "Category", "Pipes" } }),
                    new ClashObject("s-401", null, null, null, new List<string> { "Project" }, new Dictionary<string, string>()),
                    "guid-004"),
                hvac,
                structure);

            // occ0 is reused as two adjacent slots to prove duplicate slots survive round-trip.
            var occurrences = new List<ClashOccurrence> { occ0, occ0, occ2, occ3 };

            return new CoordinationRun(manifest, occurrences);
        }

        private static Dictionary<string, string> NonOrdinalProperties()
        {
            // Insert deliberately out of ordinal order; the serializer must canonicalize by ordinal key.
            var properties = new Dictionary<string, string>();
            properties["Item Source File"] = "Sigma_Main_R04.nwc";
            properties["Element Id"] = "s-100";
            properties["Category"] = "Structural Framing";
            return properties;
        }

        private static string NewTempDir()
        {
            string dir = Path.Combine(Path.GetTempPath(), "orzio-snapshot-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            return dir;
        }
    }
}
