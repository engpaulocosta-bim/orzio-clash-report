using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Interactivity;
using OrzioClashReport.Launcher.Desktop.ViewModels;
using OrzioClashReport.Launcher.Infrastructure.Updates;

namespace OrzioClashReport.Launcher.Desktop.Views
{
    public partial class SettingsView : UserControl
    {
        private static readonly DesktopUpdateService UpdateService = new DesktopUpdateService();

        public SettingsView()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private async void CheckUpdates_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is not Button button || DataContext is not SettingsViewModel settings)
            {
                return;
            }

            TextBlock? status = this.FindControl<TextBlock>("UpdateStatus");
            if (status is null || !button.IsEnabled)
            {
                return;
            }

            button.IsEnabled = false;
            try
            {
                status.Text = "A procurar atualizações oficiais no GitHub...";
                DesktopUpdateCheckResult result = await UpdateService.CheckAsync(settings.LauncherVersion);
                if (!result.UpdateAvailable || result.Update is null)
                {
                    status.Text = "Esta é a versão mais recente disponível.";
                    return;
                }

                DesktopUpdateManifest update = result.Update;
                status.Text = $"Nova versão disponível: {update.Version}.";
                if (TopLevel.GetTopLevel(this) is not Window owner)
                {
                    status.Text = "Não foi possível apresentar a confirmação da atualização.";
                    return;
                }

                bool accepted = await ConfirmInstallAsync(owner, settings.LauncherVersion, update.Version);
                if (!accepted)
                {
                    status.Text = "Atualização disponível. A instalação foi adiada.";
                    return;
                }

                status.Text = "A descarregar o instalador e a verificar o SHA-256...";
                await UpdateService.DownloadAndStartInstallerAsync(update);
                status.Text = "Instalador validado e iniciado. Conclua o assistente de instalação. Guarde o trabalho antes de fechar a aplicação.";
            }
            catch (Exception exception) when (
                exception is HttpRequestException or IOException or JsonException or UnauthorizedAccessException
                or InvalidDataException or InvalidOperationException or ArgumentException
                or PlatformNotSupportedException or TaskCanceledException)
            {
                status.Text = "Não foi possível concluir a atualização: " + exception.Message;
            }
            finally
            {
                button.IsEnabled = true;
            }
        }

        private static Task<bool> ConfirmInstallAsync(Window owner, string installedVersion, string availableVersion)
        {
            var dialog = new Window
            {
                Title = "Atualização do Orzio Clash Report",
                Width = 470,
                SizeToContent = SizeToContent.Height,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                ShowInTaskbar = false,
            };

            var content = new StackPanel { Margin = new Thickness(22), Spacing = 16 };
            content.Children.Add(new TextBlock
            {
                Text = $"Versão instalada: {installedVersion}\nNova versão: {availableVersion}\n\nDeseja descarregar e instalar a atualização agora? Termine as operações em curso e guarde o seu trabalho antes de avançar.",
                TextWrapping = TextWrapping.Wrap
            });

            var actions = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10,
                HorizontalAlignment = HorizontalAlignment.Right,
            };
            var cancel = new Button { Content = "Agora não" };
            var install = new Button { Content = "Instalar atualização", IsDefault = true };
            cancel.Click += (_, _) => dialog.Close(false);
            install.Click += (_, _) => dialog.Close(true);
            actions.Children.Add(cancel);
            actions.Children.Add(install);
            content.Children.Add(actions);
            dialog.Content = content;
            return dialog.ShowDialog<bool>(owner);
        }
    }
}
