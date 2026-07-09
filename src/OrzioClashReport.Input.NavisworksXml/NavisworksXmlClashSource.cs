using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using OrzioClashReport.Core.Abstractions;
using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Input.NavisworksXml
{
    /// <summary>Implements <see cref="IClashSource"/> by parsing a Navisworks Clash Detective XML export.</summary>
    public sealed class NavisworksXmlClashSource : IClashSource
    {
        private readonly string _filePath;
        private readonly IAppLog _log;

        public NavisworksXmlClashSource(string filePath, IAppLog log)
        {
            _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public ClashReportDocument Read()
        {
            XDocument document;
            try
            {
                document = XDocument.Load(_filePath);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to read clash export '{_filePath}': {ex.Message}", ex);
            }

            var exchange = document.Root;
            if (exchange == null || exchange.Name.LocalName != "exchange")
            {
                throw new InvalidOperationException(
                    $"'{_filePath}' is not a valid Clash Detective export: missing root <exchange> element.");
            }

            string? sourceName = (string?)exchange.Attribute("filename");

            var batches = exchange
                .Elements("batchtest")
                .Elements("clashtests")
                .Elements("clashtest")
                .Select(ParseClashTest)
                .ToList();

            return new ClashReportDocument(sourceName, null, batches);
        }

        private ClashBatch ParseClashTest(XElement clashTestElement)
        {
            string? name = (string?)clashTestElement.Attribute("name");
            double? tolerance = (double?)clashTestElement.Attribute("tolerance");

            var clashes = clashTestElement
                .Element("clashresults")
                ?.Elements("clashresult")
                .Select(ParseClashResult)
                .ToList() ?? new List<ClashResult>();

            return new ClashBatch(name, tolerance, clashes);
        }

        private ClashResult ParseClashResult(XElement clashResultElement)
        {
            string? name = (string?)clashResultElement.Attribute("name");
            string? guid = (string?)clashResultElement.Attribute("guid");
            double? distance = (double?)clashResultElement.Attribute("distance");

            string? statusText = (string?)clashResultElement.Element("resultstatus")
                ?? (string?)clashResultElement.Attribute("status");
            ClashStatus status = ParseStatus(statusText);

            ClashPoint? point = null;
            var pos3f = clashResultElement.Element("clashpoint")?.Element("pos3f");
            if (pos3f != null)
            {
                double x = (double?)pos3f.Attribute("x") ?? 0;
                double y = (double?)pos3f.Attribute("y") ?? 0;
                double z = (double?)pos3f.Attribute("z") ?? 0;
                point = new ClashPoint(x, y, z);
            }

            var clashObjectElements = clashResultElement
                .Element("clashobjects")
                ?.Elements("clashobject")
                .ToList() ?? new List<XElement>();

            if (clashObjectElements.Count < 2)
            {
                throw new InvalidOperationException(
                    $"Clash result '{name ?? guid ?? "(unnamed)"}' in '{_filePath}' does not have two clash objects.");
            }

            if (clashObjectElements.Count > 2)
            {
                _log.Warn(
                    $"Clash result '{name ?? guid ?? "(unnamed)"}' has {clashObjectElements.Count} clash objects; using the first two.");
            }

            var elementA = ParseClashObject(clashObjectElements[0], name ?? guid);
            var elementB = ParseClashObject(clashObjectElements[1], name ?? guid);

            return new ClashResult(name, status, distance, null, point, elementA, elementB, guid);
        }

        private ClashObject ParseClashObject(XElement clashObjectElement, string? clashLabel)
        {
            string? elementId = clashObjectElement
                .Elements("objectattribute")
                .FirstOrDefault(a => string.Equals((string?)a.Element("name"), "GUID", StringComparison.Ordinal))
                ?.Element("value")
                ?.Value;

            if (string.IsNullOrEmpty(elementId))
            {
                _log.Error(
                    $"Clash object in clash '{clashLabel ?? "(unnamed)"}' of '{_filePath}' has no GUID objectattribute.");
                throw new InvalidOperationException(
                    $"Clash object in clash '{clashLabel ?? "(unnamed)"}' of '{_filePath}' has no GUID objectattribute.");
            }

            string? level = (string?)clashObjectElement.Element("layer");

            var pathHierarchy = clashObjectElement
                .Element("pathlink")
                ?.Elements("node")
                .Select(n => n.Value)
                .ToList();

            var properties = clashObjectElement
                .Element("smarttags")
                ?.Elements("smarttag")
                .Select(t => new
                {
                    Name = (string?)t.Element("name"),
                    Value = (string?)t.Element("value")
                })
                .Where(t => !string.IsNullOrEmpty(t.Name))
                .GroupBy(t => t.Name!)
                .ToDictionary(g => g.Key, g => g.First().Value ?? string.Empty)
                ?? new Dictionary<string, string>();

            return new ClashObject(elementId!, null, level, null, pathHierarchy, properties);
        }

        private ClashStatus ParseStatus(string? statusText)
        {
            if (string.IsNullOrWhiteSpace(statusText))
            {
                return ClashStatus.Unknown;
            }

            if (Enum.TryParse(statusText, ignoreCase: true, out ClashStatus status))
            {
                return status;
            }

            _log.Warn($"Unrecognized clash status '{statusText}' in '{_filePath}'; mapped to Unknown.");
            return ClashStatus.Unknown;
        }
    }
}
