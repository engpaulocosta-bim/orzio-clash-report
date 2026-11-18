using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace OrzioClashReport.Launcher.Tests
{
    public sealed class ThirdPartyInventoryTests
    {
        [Fact]
        public void InventoryCoversEveryResolvedDesktopPackageVersion()
        {
            string root = RepositoryLayout.RootDirectory;
            using JsonDocument inventory = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "licenses", "inventory.json")));
            var covered = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (JsonElement component in inventory.RootElement.GetProperty("components").EnumerateArray())
            {
                covered.Add(component.GetProperty("package").GetString() + "/" + component.GetProperty("version").GetString());
                foreach (JsonElement notice in component.GetProperty("notices").EnumerateArray())
                {
                    Assert.True(File.Exists(Path.Combine(root, notice.GetString()!)), "Missing notice for " + component.GetProperty("package").GetString());
                }
            }

            using JsonDocument assets = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "src", "OrzioClashReport.Launcher.Desktop", "obj", "project.assets.json")));
            foreach (JsonProperty library in assets.RootElement.GetProperty("libraries").EnumerateObject())
            {
                if (library.Value.GetProperty("type").GetString() == "package")
                {
                    Assert.Contains(library.Name, covered);
                }
            }
        }
    }
}
