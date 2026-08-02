using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using OrzioClashReport.Launcher.Desktop.ViewModels;

namespace OrzioClashReport.Launcher.Desktop.Views
{
    public sealed partial class QuickReportView : UserControl
    {
        public QuickReportView()
        {
            AvaloniaXamlLoader.Load(this);

            AddHandler(DragDrop.DragOverEvent, OnDragOver);
            AddHandler(DragDrop.DropEvent, OnDrop);
        }

        private static void OnDragOver(object? sender, DragEventArgs e)
        {
            e.DragEffects = e.Data.Contains(DataFormats.Files) ? DragDropEffects.Copy : DragDropEffects.None;
        }

        /// <summary>Dropping an export is the same as choosing it in the picker.</summary>
        private void OnDrop(object? sender, DragEventArgs e)
        {
            if (DataContext is not QuickReportViewModel viewModel)
            {
                return;
            }

            IEnumerable<IStorageItem>? items = e.Data.GetFiles();
            string? path = items?
                .OfType<IStorageFile>()
                .Select(file => file.TryGetLocalPath())
                .FirstOrDefault(local => local != null);

            if (path != null)
            {
                viewModel.AcceptDroppedInput(path);
            }
        }
    }
}
