namespace OrzioClashReport.Output.Html
{
    /// <summary>
    /// The language a rendered report is written in. It affects the report's own wording only.
    /// A value the coordination record depends on — a clash status, a discipline, a clash test name,
    /// an element id — is whatever the source said it was, in every language.
    /// </summary>
    public enum ReportLanguage
    {
        English = 0,
        Portuguese = 1,
    }
}
