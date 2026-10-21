using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using OrzioClashReport.Launcher.Application.Localization;

namespace OrzioClashReport.Launcher.Desktop.Localization
{
    /// <summary>
    /// The text of one key, as something a binding can watch: <c>Text</c> returns the sentence in the active
    /// language, and a language change raises a change for it. There is one instance per key, shared by every
    /// place in the views that shows that key.
    /// </summary>
    /// <remarks>
    /// This is a plain property on purpose. Binding to an indexer that is told "the indexer changed" depends
    /// on how the toolkit reads that notification; binding to a named property does not, and it is the same
    /// path every other view-model binding in the application already uses.
    /// </remarks>
    public sealed class LocalizedText : INotifyPropertyChanged
    {
        private static readonly object Gate = new object();
        private static readonly Dictionary<string, LocalizedText> Instances =
            new Dictionary<string, LocalizedText>(System.StringComparer.Ordinal);

        private static readonly PropertyChangedEventArgs TextChanged = new PropertyChangedEventArgs(nameof(Text));

        static LocalizedText()
        {
            Loc.Changed += RaiseForAll;
        }

        private LocalizedText(string key)
        {
            Key = key;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Key { get; }

        public string Text => Loc.T(Key);

        /// <summary>The shared instance for a key. The set of keys is closed and small, so none is ever dropped.</summary>
        public static LocalizedText For(string key)
        {
            lock (Gate)
            {
                if (!Instances.TryGetValue(key, out LocalizedText? instance))
                {
                    instance = new LocalizedText(key);
                    Instances.Add(key, instance);
                }

                return instance;
            }
        }

        private static void RaiseForAll()
        {
            LocalizedText[] snapshot;

            lock (Gate)
            {
                snapshot = Instances.Values.ToArray();
            }

            foreach (LocalizedText instance in snapshot)
            {
                instance.PropertyChanged?.Invoke(instance, TextChanged);
            }
        }
    }
}
