using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using OrzioClashReport.Persistence.RunIndexJson;

namespace OrzioClashReport.Tests
{
    public sealed class RunIndexJsonTests
    {
        private static JsonRunIndexSerializer Sut() => new JsonRunIndexSerializer();

        private static RunIndexFileReplacer Replacer() => new RunIndexFileReplacer();

        private static RunIndexSnapshotPathResolver Resolver() => new RunIndexSnapshotPathResolver();

        private const string MinimalExpectedJson = """
        {
          "schemaVersion": 1,
          "snapshotPaths": [
            "runs/run-001.json",
            "runs/run-002.json"
          ]
        }
        """;

        [Fact]
        public void Serialize_MinimalValidDocument_MatchesExactExpectedJson()
        {
            var index = new RunIndexDocument(new[] { "runs/run-001.json", "runs/run-002.json" });

            string json = Sut().Serialize(index);

            Assert.Equal(MinimalExpectedJson.Replace("\r\n", "\n") + "\n", json, StringComparer.Ordinal);
        }

        [Fact]
        public void Serialize_RepeatedCalls_AreOrdinalIdentical()
        {
            var index = new RunIndexDocument(new[] { "runs/run-001.json", "../archive/run-002.json" });

            string first = Sut().Serialize(index);
            string second = Sut().Serialize(index);

            Assert.Equal(first, second, StringComparer.Ordinal);
        }

        [Fact]
        public void Serialize_UsesLfLineEndingsOnly()
        {
            var index = new RunIndexDocument(new[] { "run-001.json" });

            string json = Sut().Serialize(index);

            Assert.DoesNotContain("\r", json, StringComparison.Ordinal);
        }

        [Fact]
        public void Serialize_EndsWithExactlyOneTrailingLf()
        {
            var index = new RunIndexDocument(new[] { "run-001.json" });

            string json = Sut().Serialize(index);

            Assert.EndsWith("\n", json, StringComparison.Ordinal);
            Assert.False(json.EndsWith("\n\n", StringComparison.Ordinal));
        }

        [Fact]
        public void Save_WritesUtf8WithoutBom()
        {
            string tempDirectory = CreateTempDirectory();
            string outputPath = Path.Combine(tempDirectory, "run-index.json");

            try
            {
                Sut().Save(new RunIndexDocument(new[] { "run-001.json" }), outputPath);

                byte[] bytes = File.ReadAllBytes(outputPath);
                Assert.False(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF);
            }
            finally
            {
                DeleteFileIfExists(outputPath);
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Save_ExistingPath_IsRefused()
        {
            string tempDirectory = CreateTempDirectory();
            string outputPath = Path.Combine(tempDirectory, "run-index.json");

            try
            {
                File.WriteAllText(outputPath, "ORIGINAL");

                var ex = Assert.Throws<RunIndexFormatException>(
                    () => Sut().Save(new RunIndexDocument(new[] { "run-001.json" }), outputPath));

                Assert.Contains("Run index file already exists:", ex.Message, StringComparison.Ordinal);
            }
            finally
            {
                DeleteFileIfExists(outputPath);
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Save_ExistingByteIdenticalPath_IsStillRefused()
        {
            string tempDirectory = CreateTempDirectory();
            string outputPath = Path.Combine(tempDirectory, "run-index.json");
            var serializer = Sut();
            var index = new RunIndexDocument(new[] { "run-001.json" });

            try
            {
                File.WriteAllText(outputPath, serializer.Serialize(index), new UTF8Encoding(false));

                var ex = Assert.Throws<RunIndexFormatException>(() => serializer.Save(index, outputPath));

                Assert.Contains("Run index file already exists:", ex.Message, StringComparison.Ordinal);
            }
            finally
            {
                DeleteFileIfExists(outputPath);
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Save_MissingParentDirectory_Fails()
        {
            string tempDirectory = CreateTempDirectory();
            string outputPath = Path.Combine(tempDirectory, "missing-parent", "run-index.json");

            try
            {
                var ex = Assert.Throws<RunIndexFormatException>(
                    () => Sut().Save(new RunIndexDocument(new[] { "run-001.json" }), outputPath));

                Assert.Contains("Failed to write run index", ex.Message, StringComparison.Ordinal);
                Assert.False(File.Exists(outputPath));
            }
            finally
            {
                DeleteFileIfExists(outputPath);
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Save_SerializationFailure_CreatesNoDestinationFile()
        {
            string tempDirectory = CreateTempDirectory();
            string outputPath = Path.Combine(tempDirectory, "run-index.json");

            try
            {
                var ex = Assert.Throws<RunIndexFormatException>(
                    () => Sut().Save(new RunIndexDocument(Array.Empty<string>()), outputPath));

                Assert.Contains("at least one snapshot path reference", ex.Message, StringComparison.Ordinal);
                Assert.False(File.Exists(outputPath));
            }
            finally
            {
                DeleteFileIfExists(outputPath);
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Parse_SerializeRoundTrip_PreservesOrder()
        {
            var original = new RunIndexDocument(new[] { "../runs/run-003.json", "runs/run-001.json", "run-002.json" });

            RunIndexDocument loaded = Sut().Parse(Sut().Serialize(original));

            Assert.Equal(original.SnapshotPaths, loaded.SnapshotPaths);
        }

        [Fact]
        public void ReplaceExisting_ExistingPath_IsReplacedWithDeterministicJson()
        {
            string tempDirectory = CreateTempDirectory();
            string outputPath = Path.Combine(tempDirectory, "run-index.json");
            var serializer = Sut();
            var index = new RunIndexDocument(new[] { "runs/run-001.json", "archive/run-002.json" });

            try
            {
                File.WriteAllText(outputPath, "ORIGINAL", new UTF8Encoding(false));

                Replacer().ReplaceExisting(index, outputPath);

                Assert.Equal(serializer.Serialize(index), File.ReadAllText(outputPath), StringComparer.Ordinal);
            }
            finally
            {
                DeleteFileIfExists(outputPath);
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void ReplaceExisting_WritesUtf8WithoutBom()
        {
            string tempDirectory = CreateTempDirectory();
            string outputPath = Path.Combine(tempDirectory, "run-index.json");

            try
            {
                File.WriteAllText(outputPath, "ORIGINAL", new UTF8Encoding(false));

                Replacer().ReplaceExisting(new RunIndexDocument(new[] { "run-001.json" }), outputPath);

                byte[] bytes = File.ReadAllBytes(outputPath);
                Assert.False(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF);
            }
            finally
            {
                DeleteFileIfExists(outputPath);
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void ReplaceExisting_UsesLfLineEndingsOnly()
        {
            string tempDirectory = CreateTempDirectory();
            string outputPath = Path.Combine(tempDirectory, "run-index.json");

            try
            {
                File.WriteAllText(outputPath, "ORIGINAL", new UTF8Encoding(false));

                Replacer().ReplaceExisting(new RunIndexDocument(new[] { "run-001.json" }), outputPath);

                string json = File.ReadAllText(outputPath);
                Assert.DoesNotContain("\r", json, StringComparison.Ordinal);
            }
            finally
            {
                DeleteFileIfExists(outputPath);
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void ReplaceExisting_EndsWithExactlyOneTrailingLf()
        {
            string tempDirectory = CreateTempDirectory();
            string outputPath = Path.Combine(tempDirectory, "run-index.json");

            try
            {
                File.WriteAllText(outputPath, "ORIGINAL", new UTF8Encoding(false));

                Replacer().ReplaceExisting(new RunIndexDocument(new[] { "run-001.json" }), outputPath);

                string json = File.ReadAllText(outputPath);
                Assert.EndsWith("\n", json, StringComparison.Ordinal);
                Assert.False(json.EndsWith("\n\n", StringComparison.Ordinal));
            }
            finally
            {
                DeleteFileIfExists(outputPath);
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void ReplaceExisting_MissingDestination_IsRejected()
        {
            string tempDirectory = CreateTempDirectory();
            string outputPath = Path.Combine(tempDirectory, "run-index.json");

            try
            {
                var ex = Assert.Throws<RunIndexFormatException>(
                    () => Replacer().ReplaceExisting(new RunIndexDocument(new[] { "run-001.json" }), outputPath));

                Assert.Contains("Run index file not found:", ex.Message, StringComparison.Ordinal);
                Assert.False(File.Exists(outputPath));
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void ReplaceExisting_DestinationDirectory_IsRejected()
        {
            string tempDirectory = CreateTempDirectory();

            try
            {
                var ex = Assert.Throws<RunIndexFormatException>(
                    () => Replacer().ReplaceExisting(new RunIndexDocument(new[] { "run-001.json" }), tempDirectory));

                Assert.Contains("destination cannot be an existing directory", ex.Message, StringComparison.Ordinal);
                AssertNoReplacementTempFiles(tempDirectory);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void ReplaceExisting_MissingParentDirectory_IsRejected()
        {
            string tempDirectory = CreateTempDirectory();
            string outputPath = Path.Combine(tempDirectory, "missing-parent", "run-index.json");

            try
            {
                var ex = Assert.Throws<RunIndexFormatException>(
                    () => Replacer().ReplaceExisting(new RunIndexDocument(new[] { "run-001.json" }), outputPath));

                Assert.Contains("Run index parent directory not found:", ex.Message, StringComparison.Ordinal);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void ReplaceExisting_SerializationFailure_PreservesOriginalBytes()
        {
            string tempDirectory = CreateTempDirectory();
            string outputPath = Path.Combine(tempDirectory, "run-index.json");
            byte[] originalBytes = Encoding.UTF8.GetBytes("ORIGINAL-RUN-INDEX");

            try
            {
                File.WriteAllBytes(outputPath, originalBytes);

                var ex = Assert.Throws<RunIndexFormatException>(
                    () => Replacer().ReplaceExisting(new RunIndexDocument(Array.Empty<string>()), outputPath));

                Assert.Contains("at least one snapshot path reference", ex.Message, StringComparison.Ordinal);
                Assert.Equal(originalBytes, File.ReadAllBytes(outputPath));
            }
            finally
            {
                DeleteFileIfExists(outputPath);
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void ReplaceExisting_SerializationFailure_LeavesNoTemporaryFile()
        {
            string tempDirectory = CreateTempDirectory();
            string outputPath = Path.Combine(tempDirectory, "run-index.json");

            try
            {
                File.WriteAllText(outputPath, "ORIGINAL", new UTF8Encoding(false));

                Assert.Throws<RunIndexFormatException>(
                    () => Replacer().ReplaceExisting(new RunIndexDocument(Array.Empty<string>()), outputPath));

                AssertNoReplacementTempFiles(tempDirectory);
            }
            finally
            {
                DeleteFileIfExists(outputPath);
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void ReplaceExisting_Success_LeavesNoTemporaryFile()
        {
            string tempDirectory = CreateTempDirectory();
            string outputPath = Path.Combine(tempDirectory, "run-index.json");

            try
            {
                File.WriteAllText(outputPath, "ORIGINAL", new UTF8Encoding(false));

                Replacer().ReplaceExisting(new RunIndexDocument(new[] { "run-001.json" }), outputPath);

                AssertNoReplacementTempFiles(tempDirectory);
            }
            finally
            {
                DeleteFileIfExists(outputPath);
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Parse_SerializeRoundTrip_PreservesDuplicateReferences()
        {
            var original = new RunIndexDocument(new[] { "runs/run-001.json", "runs/run-001.json" });

            RunIndexDocument loaded = Sut().Parse(Sut().Serialize(original));

            Assert.Equal(2, loaded.SnapshotPaths.Count);
            Assert.Equal("runs/run-001.json", loaded.SnapshotPaths[0]);
            Assert.Equal("runs/run-001.json", loaded.SnapshotPaths[1]);
        }

        [Fact]
        public void RunIndexDocument_ConstructorInputMutation_DoesNotAffectDocument()
        {
            var source = new[] { "runs/run-001.json", "runs/run-002.json" };

            var index = new RunIndexDocument(source);
            source[0] = "runs/mutated.json";

            Assert.Equal("runs/run-001.json", index.SnapshotPaths[0]);
        }

        [Fact]
        public void RunIndexDocument_SnapshotPaths_CannotBeCastToArray()
        {
            var index = new RunIndexDocument(new[] { "runs/run-001.json" });

            string[]? exposedArray = index.SnapshotPaths as string[];

            Assert.Null(exposedArray);
        }

        [Fact]
        public void RunIndexDocument_SnapshotPaths_IListMutation_IsRefused()
        {
            var index = new RunIndexDocument(new[] { "runs/run-001.json", "runs/run-002.json" });
            IList<string> snapshotPaths = Assert.IsAssignableFrom<IList<string>>(index.SnapshotPaths);

            Assert.Throws<NotSupportedException>(() => snapshotPaths[0] = "runs/mutated.json");
            Assert.Equal("runs/run-001.json", index.SnapshotPaths[0]);
        }

        [Fact]
        public void Serialize_AfterAttemptedExternalMutation_RemainsOrdinalIdentical()
        {
            var index = new RunIndexDocument(new[] { "runs/run-001.json", "runs/run-002.json" });
            IList<string> snapshotPaths = Assert.IsAssignableFrom<IList<string>>(index.SnapshotPaths);

            string before = Sut().Serialize(index);

            Assert.Throws<NotSupportedException>(() => snapshotPaths[0] = "runs/mutated.json");

            string after = Sut().Serialize(index);
            Assert.Equal(before, after, StringComparer.Ordinal);
        }

        [Fact]
        public void Parse_MissingSchemaVersion_IsRejected()
        {
            string json = """
            {
              "snapshotPaths": [
                "run-001.json"
              ]
            }
            """;

            var ex = Assert.Throws<RunIndexFormatException>(() => Sut().Parse(json));
            Assert.Contains("'schemaVersion' is required", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void Parse_SchemaVersionZero_IsRejected()
        {
            Assert.Throws<RunIndexFormatException>(() => Sut().Parse(BuildJson("0", "[\"run-001.json\"]")));
        }

        [Fact]
        public void Parse_SchemaVersionTwo_IsRejected()
        {
            Assert.Throws<RunIndexFormatException>(() => Sut().Parse(BuildJson("2", "[\"run-001.json\"]")));
        }

        [Fact]
        public void Parse_WrongPropertyCasing_IsRejected()
        {
            string json = """
            {
              "schemaVersion": 1,
              "SnapshotPaths": [
                "run-001.json"
              ]
            }
            """;

            Assert.Throws<RunIndexFormatException>(() => Sut().Parse(json));
        }

        [Fact]
        public void Parse_UnknownRootProperty_IsRejected()
        {
            string json = """
            {
              "schemaVersion": 1,
              "snapshotPaths": [
                "run-001.json"
              ],
              "unknownRoot": true
            }
            """;

            Assert.Throws<RunIndexFormatException>(() => Sut().Parse(json));
        }

        [Fact]
        public void Parse_DuplicateSchemaVersion_IsRejected()
        {
            string json = """
            {
              "schemaVersion": 1,
              "schemaVersion": 1,
              "snapshotPaths": [
                "run-001.json"
              ]
            }
            """;

            var ex = Assert.Throws<RunIndexFormatException>(() => Sut().Parse(json));
            Assert.Contains("Duplicate JSON property 'schemaVersion' at root.", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void Parse_DuplicateSnapshotPathsProperty_IsRejected()
        {
            string json = """
            {
              "schemaVersion": 1,
              "snapshotPaths": [
                "run-001.json"
              ],
              "snapshotPaths": [
                "run-002.json"
              ]
            }
            """;

            var ex = Assert.Throws<RunIndexFormatException>(() => Sut().Parse(json));
            Assert.Contains("Duplicate JSON property 'snapshotPaths' at root.", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void Parse_MissingSnapshotPaths_IsRejected()
        {
            string json = """
            {
              "schemaVersion": 1
            }
            """;

            var ex = Assert.Throws<RunIndexFormatException>(() => Sut().Parse(json));
            Assert.Contains("'snapshotPaths' is required", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void Parse_NullSnapshotPaths_IsRejected()
        {
            string json = """
            {
              "schemaVersion": 1,
              "snapshotPaths": null
            }
            """;

            var ex = Assert.Throws<RunIndexFormatException>(() => Sut().Parse(json));
            Assert.Contains("'snapshotPaths' is required", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void Parse_EmptySnapshotPaths_IsRejected()
        {
            string json = """
            {
              "schemaVersion": 1,
              "snapshotPaths": []
            }
            """;

            var ex = Assert.Throws<RunIndexFormatException>(() => Sut().Parse(json));
            Assert.Contains("must contain at least one snapshot path reference", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void Parse_NullEntry_IsRejected()
        {
            string json = """
            {
              "schemaVersion": 1,
              "snapshotPaths": [
                null
              ]
            }
            """;

            var ex = Assert.Throws<RunIndexFormatException>(() => Sut().Parse(json));
            Assert.Contains("'snapshotPaths[0]' cannot be null", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void Parse_EmptyEntry_IsRejected()
        {
            string json = BuildJson("1", "[\"\"]");
            Assert.Throws<RunIndexFormatException>(() => Sut().Parse(json));
        }

        [Fact]
        public void Parse_WhitespaceEntry_IsRejected()
        {
            string json = BuildJson("1", "[\"   \"]");
            Assert.Throws<RunIndexFormatException>(() => Sut().Parse(json));
        }

        [Fact]
        public void Parse_AbsoluteUnixReference_IsRejected()
        {
            Assert.Throws<RunIndexFormatException>(() => Sut().Parse(BuildJson("1", "[\"/runs/a.json\"]")));
        }

        [Fact]
        public void Parse_WindowsDriveReferenceWithForwardSlash_IsRejected()
        {
            Assert.Throws<RunIndexFormatException>(() => Sut().Parse(BuildJson("1", "[\"C:/runs/a.json\"]")));
        }

        [Fact]
        public void Parse_WindowsDriveReferenceWithBackslash_IsRejected()
        {
            Assert.Throws<RunIndexFormatException>(() => Sut().Parse(BuildJson("1", "[\"C:\\\\runs\\\\a.json\"]")));
        }

        [Fact]
        public void Parse_WindowsDriveRelativeReference_IsRejected()
        {
            Assert.Throws<RunIndexFormatException>(() => Sut().Parse(BuildJson("1", "[\"C:run.json\"]")));
        }

        [Fact]
        public void Parse_WindowsDriveRelativeReferenceWithLowercaseDrive_IsRejected()
        {
            Assert.Throws<RunIndexFormatException>(() => Sut().Parse(BuildJson("1", "[\"d:archive/run.json\"]")));
        }

        [Fact]
        public void Parse_UncReference_IsRejected()
        {
            Assert.Throws<RunIndexFormatException>(() => Sut().Parse(BuildJson("1", "[\"//server/share/run.json\"]")));
        }

        [Fact]
        public void Parse_BackslashSeparatedRelativeReference_IsRejected()
        {
            Assert.Throws<RunIndexFormatException>(() => Sut().Parse(BuildJson("1", "[\"runs\\\\run.json\"]")));
        }

        [Fact]
        public void Parse_TrailingSlashReference_IsRejected()
        {
            Assert.Throws<RunIndexFormatException>(() => Sut().Parse(BuildJson("1", "[\"runs/\"]")));
        }

        [Fact]
        public void Parse_SimpleFilename_IsAccepted()
        {
            RunIndexDocument loaded = Sut().Parse(BuildJson("1", "[\"run.json\"]"));
            Assert.Equal("run.json", loaded.SnapshotPaths[0]);
        }

        [Fact]
        public void Parse_NestedForwardSlashReference_IsAccepted()
        {
            RunIndexDocument loaded = Sut().Parse(BuildJson("1", "[\"runs/run.json\"]"));
            Assert.Equal("runs/run.json", loaded.SnapshotPaths[0]);
        }

        [Fact]
        public void Parse_ParentDirectoryReference_IsAccepted()
        {
            RunIndexDocument loaded = Sut().Parse(BuildJson("1", "[\"../snapshots/run.json\"]"));
            Assert.Equal("../snapshots/run.json", loaded.SnapshotPaths[0]);
        }

        [Fact]
        public void Load_MissingFile_ProducesContextualRunIndexFormatException()
        {
            string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");

            var ex = Assert.Throws<RunIndexFormatException>(() => Sut().Load(path));

            Assert.Contains("Run index file not found:", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void Parse_EmptyOrWhitespaceJson_IsRejected()
        {
            Assert.Throws<RunIndexFormatException>(() => Sut().Parse("   \n\t "));
        }

        [Fact]
        public void Resolver_SnapshotBesideIndex_CreatesSimpleFilename()
        {
            string tempDirectory = CreateTempDirectory();

            try
            {
                string indexPath = Path.Combine(tempDirectory, "run-index.json");
                string snapshotPath = Path.Combine(tempDirectory, "run-001.json");

                string reference = Resolver().CreateReference(indexPath, snapshotPath);

                Assert.Equal("run-001.json", reference);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Resolver_SnapshotBelowIndexDirectory_UsesForwardSlashNestedReference()
        {
            string tempDirectory = CreateTempDirectory();

            try
            {
                string indexPath = Path.Combine(tempDirectory, "run-index.json");
                string snapshotPath = Path.Combine(tempDirectory, "runs", "run-001.json");

                string reference = Resolver().CreateReference(indexPath, snapshotPath);

                Assert.Equal("runs/run-001.json", reference);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Resolver_SnapshotAboveIndexDirectory_UsesParentDirectoryReference()
        {
            string tempDirectory = CreateTempDirectory();
            string historyDirectory = Path.Combine(tempDirectory, "history");

            try
            {
                Directory.CreateDirectory(historyDirectory);
                string indexPath = Path.Combine(historyDirectory, "run-index.json");
                string snapshotPath = Path.Combine(tempDirectory, "snapshots", "run-001.json");

                string reference = Resolver().CreateReference(indexPath, snapshotPath);

                Assert.Equal("../snapshots/run-001.json", reference);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Resolver_CreateReference_DoesNotReturnAbsolutePath()
        {
            string tempDirectory = CreateTempDirectory();

            try
            {
                string indexPath = Path.Combine(tempDirectory, "run-index.json");
                string snapshotPath = Path.Combine(tempDirectory, "runs", "run-001.json");

                string reference = Resolver().CreateReference(indexPath, snapshotPath);

                Assert.False(Path.IsPathRooted(reference));
                Assert.DoesNotContain(":", reference, StringComparison.Ordinal);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Resolver_CreateReference_PersistsForwardSlashSeparator()
        {
            string tempDirectory = CreateTempDirectory();

            try
            {
                string indexPath = Path.Combine(tempDirectory, "run-index.json");
                string snapshotPath = Path.Combine(tempDirectory, "runs", "nested", "run-001.json");

                string reference = Resolver().CreateReference(indexPath, snapshotPath);

                Assert.DoesNotContain("\\", reference, StringComparison.Ordinal);
                Assert.Contains("/", reference, StringComparison.Ordinal);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Resolver_ResolveReference_ReturnsOriginalFullSnapshotPath()
        {
            string tempDirectory = CreateTempDirectory();

            try
            {
                string indexPath = Path.Combine(tempDirectory, "run-index.json");
                string snapshotPath = Path.Combine(tempDirectory, "runs", "run-001.json");
                string reference = Resolver().CreateReference(indexPath, snapshotPath);

                string resolved = Resolver().ResolveReference(indexPath, reference);

                AssertPathsEqual(snapshotPath, resolved);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Resolver_CreateAndResolveReference_RoundTrips()
        {
            string tempDirectory = CreateTempDirectory();

            try
            {
                string indexPath = Path.Combine(tempDirectory, "history", "run-index.json");
                string snapshotPath = Path.Combine(tempDirectory, "snapshots", "run-001.json");

                string reference = Resolver().CreateReference(indexPath, snapshotPath);
                string resolved = Resolver().ResolveReference(indexPath, reference);

                AssertPathsEqual(snapshotPath, resolved);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Resolver_NullIndexPath_IsRejected()
        {
            Assert.Throws<ArgumentNullException>(() => Resolver().CreateReference(null!, "snapshot.json"));
        }

        [Fact]
        public void Resolver_WhitespaceIndexPath_IsRejected()
        {
            Assert.Throws<RunIndexFormatException>(() => Resolver().CreateReference("   ", "snapshot.json"));
        }

        [Fact]
        public void Resolver_NullSnapshotPath_IsRejected()
        {
            Assert.Throws<ArgumentNullException>(() => Resolver().CreateReference("run-index.json", null!));
        }

        [Fact]
        public void Resolver_WhitespaceSnapshotPath_IsRejected()
        {
            Assert.Throws<RunIndexFormatException>(() => Resolver().CreateReference("run-index.json", "   "));
        }

        [Fact]
        public void Resolver_InvalidPersistedReference_IsRejectedByResolveReference()
        {
            Assert.Throws<RunIndexFormatException>(() => Resolver().ResolveReference("run-index.json", "runs\\run.json"));
        }

        [Fact]
        public void Resolver_DriveRelativePersistedReference_IsRejectedByResolveReference()
        {
            Assert.Throws<RunIndexFormatException>(() => Resolver().ResolveReference("run-index.json", "C:run.json"));
        }

        [Fact]
        public void Resolver_DoesNotRequireSnapshotFileExistence()
        {
            string tempDirectory = CreateTempDirectory();

            try
            {
                string indexPath = Path.Combine(tempDirectory, "run-index.json");
                string snapshotPath = Path.Combine(tempDirectory, "runs", "missing.json");

                string reference = Resolver().CreateReference(indexPath, snapshotPath);

                Assert.Equal("runs/missing.json", reference);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        private static string BuildJson(string schemaVersion, string snapshotPathsJson) =>
            $$"""
            {
              "schemaVersion": {{schemaVersion}},
              "snapshotPaths": {{snapshotPathsJson}}
            }
            """;

        private static void AssertPathsEqual(string expectedPath, string actualPath)
        {
            string expectedFullPath = Path.GetFullPath(expectedPath);
            string actualFullPath = Path.GetFullPath(actualPath);
            StringComparer comparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
            Assert.Equal(expectedFullPath, actualFullPath, comparer);
        }

        private static string CreateTempDirectory()
        {
            string path = Path.Combine(Path.GetTempPath(), $"orzio-clash-report-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(path);
            return path;
        }

        private static void DeleteFileIfExists(string path)
        {
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private static void DeleteDirectoryIfExists(string path)
        {
            if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }

        private static void AssertNoReplacementTempFiles(string directoryPath)
        {
            string[] temporaryFiles = Directory.Exists(directoryPath)
                ? Directory.GetFiles(directoryPath, ".run-index-replace-*.tmp", SearchOption.TopDirectoryOnly)
                : Array.Empty<string>();

            Assert.Empty(temporaryFiles);
        }
    }
}
