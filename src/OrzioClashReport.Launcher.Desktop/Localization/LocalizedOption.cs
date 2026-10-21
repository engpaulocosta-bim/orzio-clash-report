using System;
using OrzioClashReport.Launcher.Application.Localization;

namespace OrzioClashReport.Launcher.Desktop.Localization
{
    /// <summary>
    /// One entry of a short list the user picks from, such as the theme or the language. The label is
    /// looked up each time it is read, so an open list follows the interface language. The base type is
    /// not generic so a view can name it in <c>x:DataType</c>.
    /// </summary>
    public abstract class LocalizedOption : LocalizedObservableObject
    {
        private readonly string _labelKey;

        protected LocalizedOption(string labelKey)
        {
            _labelKey = labelKey ?? throw new ArgumentNullException(nameof(labelKey));
        }

        public string Label => Loc.T(_labelKey);
    }

    /// <summary>A <see cref="LocalizedOption"/> carrying the value that is stored when it is chosen.</summary>
    public sealed class LocalizedOption<T> : LocalizedOption
        where T : notnull
    {
        public LocalizedOption(T value, string labelKey)
            : base(labelKey)
        {
            Value = value;
        }

        public T Value { get; }
    }
}
