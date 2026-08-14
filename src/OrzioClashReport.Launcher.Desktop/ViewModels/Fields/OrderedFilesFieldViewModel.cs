using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrzioClashReport.Launcher.Application.Operations;
using OrzioClashReport.Launcher.Contracts.Results;
using OrzioClashReport.Launcher.Desktop.Platform;

namespace OrzioClashReport.Launcher.Desktop.ViewModels.Fields
{
    /// <summary>
    /// An explicitly ordered list of files. The user's order is the only order: nothing here sorts,
    /// and a repeat is kept and reported rather than removed. Moving an entry is the only way its
    /// position changes.
    /// </summary>
    public sealed partial class OrderedFilesFieldViewModel : OperationFieldViewModel
    {
        private readonly OrderedFileList _files = new OrderedFileList();
        private readonly IFileDialogService _fileDialogService;
        private readonly FilePickerFileKind _kind;
        private readonly Action? _onChanged;

        [ObservableProperty]
        private int _selectedIndex = -1;

        public OrderedFilesFieldViewModel(
            string label,
            string description,
            IFileDialogService fileDialogService,
            FilePickerFileKind kind,
            Action? onChanged = null)
            : base(label, description)
        {
            _fileDialogService = fileDialogService ?? throw new ArgumentNullException(nameof(fileDialogService));
            _kind = kind;
            _onChanged = onChanged;
        }

        public ObservableCollection<OrderedFileEntryViewModel> Entries { get; } =
            new ObservableCollection<OrderedFileEntryViewModel>();

        public ObservableCollection<string> Warnings { get; } = new ObservableCollection<string>();

        public IReadOnlyList<string> Paths => _files.Paths;

        public override bool IsComplete => _files.Count > 0;

        [RelayCommand]
        private async Task AddAsync()
        {
            IReadOnlyList<string> picked = await _fileDialogService
                .PickOpenFilesAsync("Escolher ficheiros", _kind, LastDirectory())
                .ConfigureAwait(true);

            if (picked.Count == 0)
            {
                return;
            }

            // Appended in the order the picker returned them, then reorderable by hand. The launcher
            // never rearranges the selection on the user's behalf.
            _files.AddRange(picked);
            Refresh();
        }

        [RelayCommand]
        private void Remove(OrderedFileEntryViewModel? entry)
        {
            if (entry == null)
            {
                return;
            }

            _files.RemoveAt(entry.Index);
            Refresh();
        }

        [RelayCommand]
        private void MoveUp(OrderedFileEntryViewModel? entry)
        {
            if (entry != null && _files.MoveUp(entry.Index))
            {
                Refresh();
            }
        }

        [RelayCommand]
        private void MoveDown(OrderedFileEntryViewModel? entry)
        {
            if (entry != null && _files.MoveDown(entry.Index))
            {
                Refresh();
            }
        }

        [RelayCommand]
        private void Clear()
        {
            _files.Clear();
            Refresh();
        }

        private void Refresh()
        {
            Entries.Clear();

            IReadOnlyList<string> paths = _files.Paths;
            for (int i = 0; i < paths.Count; i++)
            {
                Entries.Add(new OrderedFileEntryViewModel(i, paths[i]));
            }

            Warnings.Clear();
            foreach (LauncherWarning warning in _files.Warnings())
            {
                Warnings.Add(warning.Message);
            }

            OnPropertyChanged(nameof(IsComplete));
            _onChanged?.Invoke();
        }

        private string? LastDirectory()
        {
            IReadOnlyList<string> paths = _files.Paths;
            if (paths.Count == 0)
            {
                return null;
            }

            string? directory = Path.GetDirectoryName(paths[paths.Count - 1]);
            return string.IsNullOrEmpty(directory) ? null : directory;
        }
    }

    /// <summary>One entry in an ordered list, carrying its declared position.</summary>
    public sealed class OrderedFileEntryViewModel
    {
        public OrderedFileEntryViewModel(int index, string path)
        {
            Index = index;
            Path = path;
            FileName = System.IO.Path.GetFileName(path);
            Position = (index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        public int Index { get; }

        public string Path { get; }

        public string FileName { get; }

        public string Position { get; }
    }
}
