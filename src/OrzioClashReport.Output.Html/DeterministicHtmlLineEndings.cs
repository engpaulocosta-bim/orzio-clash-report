using System;

namespace OrzioClashReport.Output.Html
{
    internal static class DeterministicHtmlLineEndings
    {
        public static string NormalizeTemplateLiteral(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            return value.Replace("\r\n", "\n").Replace("\r", "\n");
        }
    }
}
