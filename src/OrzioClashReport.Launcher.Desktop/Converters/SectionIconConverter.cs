using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
using OrzioClashReport.Launcher.Application.Presentation;

namespace OrzioClashReport.Launcher.Desktop.Converters
{
    /// <summary>
    /// Resolves the vector icon for a navigation section from the design tokens (<c>OrzioIcon{Section}</c>).
    /// The icon only accompanies the section label, which stays visible or available as a tooltip.
    /// </summary>
    public sealed class SectionIconConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not LauncherSection section || Avalonia.Application.Current == null)
            {
                return null;
            }

            return Avalonia.Application.Current.TryFindResource("OrzioIcon" + section, out object? resource)
                ? resource as Geometry
                : null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
