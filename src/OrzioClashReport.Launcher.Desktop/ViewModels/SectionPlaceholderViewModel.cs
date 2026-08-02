using System;

namespace OrzioClashReport.Launcher.Desktop.ViewModels
{
    /// <summary>
    /// A section whose operations are not wired to the engine yet. It states that plainly instead of
    /// showing controls that would do nothing.
    /// </summary>
    public sealed class SectionPlaceholderViewModel : ViewModelBase
    {
        private readonly string _titleKey;
        private readonly string _descriptionKey;

        public SectionPlaceholderViewModel(string titleKey, string descriptionKey)
        {
            _titleKey = titleKey ?? throw new ArgumentNullException(nameof(titleKey));
            _descriptionKey = descriptionKey ?? throw new ArgumentNullException(nameof(descriptionKey));
        }

        public string Title => Text(_titleKey);

        public string Description => Text(_descriptionKey);

        public string NotWiredNote => Text("Placeholder.NotWired");
    }
}
