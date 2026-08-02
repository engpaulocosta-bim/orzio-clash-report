using System;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using OrzioClashReport.Launcher.Desktop.ViewModels;

namespace OrzioClashReport.Launcher.Desktop
{
    public sealed partial class MainWindow : Window
    {
        private readonly CancellationTokenSource _lifetime = new CancellationTokenSource();

        public MainWindow()
        {
            AvaloniaXamlLoader.Load(this);
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            PublishWidth();
        }

        protected override void OnSizeChanged(SizeChangedEventArgs e)
        {
            base.OnSizeChanged(e);
            PublishWidth();
        }

        protected override async void OnOpened(EventArgs e)
        {
            base.OnOpened(e);

            if (DataContext is ShellViewModel shell)
            {
                // The engine state must be established before anything claims the engine is usable.
                await shell.InitializeAsync(_lifetime.Token);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _lifetime.Cancel();
            _lifetime.Dispose();
            base.OnClosed(e);
        }

        /// <summary>
        /// Forwards the window width so the shell can decide between the sidebar and the icon rail.
        /// This is presentation plumbing only; no decision is made here.
        /// </summary>
        private void PublishWidth()
        {
            if (DataContext is ShellViewModel shell)
            {
                shell.WindowWidth = Bounds.Width;
            }
        }
    }
}
