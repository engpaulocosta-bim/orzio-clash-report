namespace OrzioClashReport.Output.Html
{
    /// <summary>
    /// Every word the single-run report writes for itself, in both languages. Nothing read from the
    /// export passes through here: a clash status, a discipline, a level, a clash test name, and an
    /// element id are reproduced exactly as the source stated them.
    /// </summary>
    internal sealed class ReportTexts
    {
        private readonly ReportLanguage _language;

        internal ReportTexts(ReportLanguage language)
        {
            _language = language;
        }

        /// <summary>The value of the document's <c>lang</c> attribute, so assistive technology and
        /// hyphenation follow the language actually used.</summary>
        internal string HtmlLanguageTag => Pick("en", "pt");

        internal string DocumentTitle => Pick("Coordination clash report", "Relatório de conflitos de coordenação");

        internal string Subtitle => DocumentTitle;

        internal string SourceModelLabel => Pick("Source model", "Modelo de origem");

        internal string ClashTestsLabel => Pick("Clash tests", "Testes de conflitos");

        internal string UnitsLabel => Pick("Units", "Unidades");

        internal string GeneratedLabel => Pick("Generated", "Gerado em");

        internal string ClashTestLabel => Pick("Clash test", "Teste de conflitos");

        internal string NoLevel => Pick("(no level)", "(sem nível)");

        internal string UnnamedTest => Pick("(unnamed test)", "(teste sem nome)");

        internal string Unnamed => Pick("(unnamed)", "(sem nome)");

        internal string ColumnName => Pick("Name", "Nome");

        internal string ColumnStatus => Pick("Status", "Estado");

        internal string ColumnDistance => Pick("Distance", "Distância");

        internal string ColumnPoint => Pick("Point", "Ponto");

        internal string ColumnElementA => Pick("Element A", "Elemento A");

        internal string ColumnElementB => Pick("Element B", "Elemento B");

        /// <summary>The one sentence the whole report exists to state.</summary>
        internal string Summary(int rawCount, int groupCount) =>
            _language == ReportLanguage.Portuguese
                ? Format("{0} conflitos brutos &rarr; {1} grupos", rawCount, groupCount)
                : Format("{0} raw clashes &rarr; {1} groups", rawCount, groupCount);

        internal string GroupCount(int count) =>
            _language == ReportLanguage.Portuguese
                ? Format("{0} conflito(s)", count)
                : Format("{0} clash(es)", count);

        private string Pick(string english, string portuguese) =>
            _language == ReportLanguage.Portuguese ? portuguese : english;

        private static string Format(string format, params object[] values) =>
            string.Format(System.Globalization.CultureInfo.InvariantCulture, format, values);
    }
}
