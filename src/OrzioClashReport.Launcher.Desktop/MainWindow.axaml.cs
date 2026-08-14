using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using OrzioClashReport.Launcher.Desktop.ViewModels;

namespace OrzioClashReport.Launcher.Desktop
{
    /// <summary>
    /// The application shell window. The only thing it decides for itself is when the navigation
    /// collapses to a rail, because the window's width is the one fact only the view has.
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly double _railBreakpoint;

        public MainWindow()
        {
            AvaloniaXamlLoader.Load(this);

            _railBreakpoint = ReadBreakpoint();
        }

        protected override void OnSizeChanged(SizeChangedEventArgs e)
        {
            base.OnSizeChanged(e);

            if (DataContext is ShellViewModel shell)
            {
                shell.SetRailMode(e.NewSize.Width < _railBreakpoint);
            }
        }

        private double ReadBreakpoint()
        {
            // Fully qualified: the sibling OrzioClashReport.Launcher.Application namespace would
            // otherwise shadow the toolkit's Application type.
            if (Avalonia.Application.Current != null
                && Avalonia.Application.Current.TryFindResource("OrzioRailBreakpoint", out object? value)
                && value is double breakpoint)
            {
                return breakpoint;
            }

            // The token dictionary is always merged in App.axaml; this only guards the designer, where
            // application resources may not be loaded yet.
            return 0;
        }
    }
}
