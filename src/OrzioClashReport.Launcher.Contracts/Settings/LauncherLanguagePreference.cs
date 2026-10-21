namespace OrzioClashReport.Launcher.Contracts.Settings
{
    /// <summary>
    /// Which interface language the user asked for. <see cref="System"/> follows the operating system:
    /// Portuguese when the system language is Portuguese, English otherwise.
    /// </summary>
    /// <remarks>
    /// The language changes visible text only. Nothing the engine receives is translated: command names,
    /// option names and canonical values such as <c>ConfirmSameIdentity</c> are identical in both languages.
    /// </remarks>
    public enum LauncherLanguagePreference
    {
        System = 0,
        Portuguese = 1,
        English = 2,
    }
}
