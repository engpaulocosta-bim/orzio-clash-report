using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using OrzioClashReport.Launcher.Application.Localization;

namespace OrzioClashReport.Launcher.Desktop.Localization
{
    /// <summary>
    /// A view model whose properties may return interface text. When the interface language changes it
    /// raises "every property changed", so a label computed from a key is read again and the screen
    /// follows without being rebuilt.
    /// </summary>
    /// <remarks>
    /// Text that depends on the language must be computed when read (<c>Label =&gt; Loc.T(key)</c>), never
    /// stored. Text that was produced by an event, such as an engine error, is stored as it was written
    /// and stays in the language it was produced in.
    /// </remarks>
    public abstract class LocalizedObservableObject : ObservableObject
    {
        protected LocalizedObservableObject()
        {
            LanguageChangeRelay.Register(this);
        }

        internal void NotifyLanguageChanged() => OnPropertyChanged(string.Empty);
    }

    /// <summary>
    /// Forwards the one static language-changed event to every live <see cref="LocalizedObservableObject"/>
    /// without keeping any of them alive: a form that has been dropped is collected normally.
    /// </summary>
    internal static class LanguageChangeRelay
    {
        private static readonly object Gate = new object();
        private static readonly List<WeakReference<LocalizedObservableObject>> Subscribers =
            new List<WeakReference<LocalizedObservableObject>>();

        static LanguageChangeRelay()
        {
            Loc.Changed += Relay;
        }

        internal static void Register(LocalizedObservableObject subscriber)
        {
            lock (Gate)
            {
                Subscribers.Add(new WeakReference<LocalizedObservableObject>(subscriber));
            }
        }

        private static void Relay()
        {
            List<LocalizedObservableObject> alive = new List<LocalizedObservableObject>();

            lock (Gate)
            {
                Subscribers.RemoveAll(reference =>
                {
                    if (reference.TryGetTarget(out LocalizedObservableObject? target))
                    {
                        alive.Add(target);
                        return false;
                    }

                    return true;
                });
            }

            foreach (LocalizedObservableObject subscriber in alive)
            {
                subscriber.NotifyLanguageChanged();
            }
        }
    }
}
