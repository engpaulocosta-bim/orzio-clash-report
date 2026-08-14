namespace OrzioClashReport.Launcher.Application.Presentation
{
    /// <summary>
    /// How prominent a piece of state is. Severity chooses a colour, but it never carries the meaning
    /// on its own: every state that uses it also has a distinct glyph and a distinct text label.
    /// </summary>
    public enum LauncherSeverity
    {
        Neutral = 0,
        Positive = 1,
        Caution = 2,
        Critical = 3,
    }
}
