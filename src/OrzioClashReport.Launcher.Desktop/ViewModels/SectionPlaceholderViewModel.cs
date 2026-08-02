using System;

namespace OrzioClashReport.Launcher.Desktop.ViewModels
{
    /// <summary>
    /// A section whose operations are not wired to the engine yet. It states that plainly instead of
    /// showing controls that would do nothing.
    /// </summary>
    public sealed class SectionPlaceholderViewModel : ViewModelBase
    {
        public SectionPlaceholderViewModel(string title, string description)
        {
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Description = description ?? throw new ArgumentNullException(nameof(description));
        }

        public string Title { get; }

        public string Description { get; }
    }
}
