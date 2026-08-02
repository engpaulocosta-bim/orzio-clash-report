using CommunityToolkit.Mvvm.ComponentModel;
using OrzioClashReport.Launcher.Desktop.Localization;

namespace OrzioClashReport.Launcher.Desktop.ViewModels
{
    /// <summary>
    /// Every view model shows interface text, so every view model re-reads it when the language
    /// changes. The subscription is weak, so this never keeps a short-lived view model alive.
    /// </summary>
    public abstract class ViewModelBase : ObservableObject, ILocalizedObject
    {
        protected ViewModelBase()
        {
            Localizer.Instance.Subscribe(this);
        }

        /// <summary>Text for a key in the language currently in use.</summary>
        protected static string Text(string key) => Localizer.Instance.Get(key);

        protected static string Text(string key, params object?[] arguments) =>
            Localizer.Instance.Format(key, arguments);

        public virtual void OnLanguageChanged()
        {
            // An empty name asks every binding on this view model to re-read.
            OnPropertyChanged(string.Empty);
        }
    }

    /// <summary>
    /// How prominently a piece of state should read. It is always paired with text and a glyph, so
    /// nothing in the application is distinguishable by colour alone.
    /// </summary>
    public enum StatusSeverity
    {
        Neutral = 0,
        Positive = 1,
        Caution = 2,
        Critical = 3,
    }
}
