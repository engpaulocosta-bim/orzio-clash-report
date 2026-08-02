using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using OrzioClashReport.Launcher.Contracts.Settings;

namespace OrzioClashReport.Launcher.Desktop.Localization
{
    /// <summary>
    /// Resolves interface text for the current language and tells the interface to re-read it when
    /// the language changes.
    /// </summary>
    public sealed class Localizer : INotifyPropertyChanged
    {
        private readonly List<WeakReference<ILocalizedObject>> _subscribers =
            new List<WeakReference<ILocalizedObject>>();

        private readonly object _gate = new object();

        private InterfaceLanguage _language = InterfaceLanguage.System;

        private Localizer()
        {
        }

        public static Localizer Instance { get; } = new Localizer();

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>The chosen language, which may be <see cref="InterfaceLanguage.System"/>.</summary>
        public InterfaceLanguage Language
        {
            get => _language;
            set
            {
                if (_language == value)
                {
                    return;
                }

                _language = value;
                Resolved = Resolve(value);

                // An empty property name asks every binding on this source to re-read.
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
                NotifySubscribers();
            }
        }

        /// <summary>The language actually in use, never <see cref="InterfaceLanguage.System"/>.</summary>
        public InterfaceLanguage Resolved { get; private set; } = Resolve(InterfaceLanguage.System);

        /// <summary>Interface text for a key. An unknown key is a defect and says so loudly.</summary>
        public string this[string key] => Get(key);

        public string Get(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (!UiStrings.All.TryGetValue(key, out UiText text))
            {
                throw new KeyNotFoundException($"No interface text is defined for '{key}'.");
            }

            return Resolved == InterfaceLanguage.Portuguese ? text.Portuguese : text.English;
        }

        /// <summary>Interface text with positional arguments, formatted invariantly.</summary>
        public string Format(string key, params object?[] arguments) =>
            string.Format(CultureInfo.InvariantCulture, Get(key), arguments);

        /// <summary>
        /// Registers an object to be told when the language changes. The reference is weak, so a
        /// short-lived view model cannot be kept alive by this.
        /// </summary>
        public void Subscribe(ILocalizedObject subscriber)
        {
            if (subscriber == null)
            {
                throw new ArgumentNullException(nameof(subscriber));
            }

            lock (_gate)
            {
                _subscribers.Add(new WeakReference<ILocalizedObject>(subscriber));
            }
        }

        private void NotifySubscribers()
        {
            ILocalizedObject[] live;

            lock (_gate)
            {
                var alive = new List<ILocalizedObject>(_subscribers.Count);
                for (int i = _subscribers.Count - 1; i >= 0; i--)
                {
                    if (_subscribers[i].TryGetTarget(out ILocalizedObject? target))
                    {
                        alive.Add(target);
                    }
                    else
                    {
                        _subscribers.RemoveAt(i);
                    }
                }

                live = alive.ToArray();
            }

            foreach (ILocalizedObject subscriber in live)
            {
                subscriber.OnLanguageChanged();
            }
        }

        private static InterfaceLanguage Resolve(InterfaceLanguage language)
        {
            if (language != InterfaceLanguage.System)
            {
                return language;
            }

            // Portuguese for a Portuguese system, English for everything else. There is no third
            // fallback to guess at.
            string twoLetter = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            return string.Equals(twoLetter, "pt", StringComparison.OrdinalIgnoreCase)
                ? InterfaceLanguage.Portuguese
                : InterfaceLanguage.English;
        }
    }

    /// <summary>Something that shows interface text and must re-read it when the language changes.</summary>
    public interface ILocalizedObject
    {
        void OnLanguageChanged();
    }
}
