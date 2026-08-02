using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrzioClashReport.Launcher.Desktop.Platform;

namespace OrzioClashReport.Launcher.Desktop.ViewModels
{
    /// <summary>
    /// One labelled file field in a typed form. The form holds paths, never fragments of a command
    /// line, and the human always picks through the platform's own dialog.
    /// </summary>
    public sealed partial class FileFieldViewModel : ViewModelBase, IFormField
    {
        private readonly IFileDialogs _dialogs;
        private readonly PickedFileKind _kind;
        private readonly bool _isDestination;
        private readonly Func<string>? _suggestedFileName;

        [ObservableProperty]
        private string? _path;

        private FileFieldViewModel(
            string label,
            string hint,
            IFileDialogs dialogs,
            PickedFileKind kind,
            bool isDestination,
            Func<string>? suggestedFileName)
        {
            Label = label ?? throw new ArgumentNullException(nameof(label));
            Hint = hint ?? throw new ArgumentNullException(nameof(hint));
            _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
            _kind = kind;
            _isDestination = isDestination;
            _suggestedFileName = suggestedFileName;
        }

        public string Label { get; }

        /// <summary>What this file is, in the coordinator's terms rather than the engine's.</summary>
        public string Hint { get; }

        public string? FileName => Path == null ? null : System.IO.Path.GetFileName(Path);

        public bool HasValue => Path != null;

        public string ButtonText => _isDestination ? "Escolher destino…" : "Escolher ficheiro…";

        public static FileFieldViewModel Input(
            string label, string hint, IFileDialogs dialogs, PickedFileKind kind)
        {
            return new FileFieldViewModel(label, hint, dialogs, kind, isDestination: false, suggestedFileName: null);
        }

        public static FileFieldViewModel Destination(
            string label, string hint, IFileDialogs dialogs, PickedFileKind kind, Func<string> suggestedFileName)
        {
            return new FileFieldViewModel(label, hint, dialogs, kind, isDestination: true, suggestedFileName);
        }

        [RelayCommand]
        private async Task PickAsync()
        {
            string? picked = _isDestination
                ? await _dialogs
                    .PickSaveFileAsync(Label, _kind, _suggestedFileName?.Invoke() ?? string.Empty)
                    .ConfigureAwait(true)
                : await _dialogs.PickOpenFileAsync(Label, _kind).ConfigureAwait(true);

            if (picked != null)
            {
                Path = System.IO.Path.GetFullPath(picked);
            }
        }

        [RelayCommand]
        private void Clear() => Path = null;

        partial void OnPathChanged(string? value)
        {
            _ = value;
            OnPropertyChanged(nameof(FileName));
            OnPropertyChanged(nameof(HasValue));
            Changed?.Invoke();
        }

        /// <summary>Raised whenever the chosen file changes, so a form can re-evaluate its command.</summary>
        public event Action? Changed;
    }

    /// <summary>One entry in an explicitly ordered list of files.</summary>
    public sealed partial class OrderedFileViewModel : ViewModelBase
    {
        public OrderedFileViewModel(string path)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
        }

        public string Path { get; }

        public string FileName => System.IO.Path.GetFileName(Path);

        /// <summary>Set when the same file appears earlier in the list. A warning, never a removal.</summary>
        [ObservableProperty]
        private bool _isDuplicate;

        [ObservableProperty]
        private int _position;
    }

    /// <summary>
    /// An explicitly ordered list of files. The declared order is the only sequence authority: this
    /// list never sorts by date, name, or revision, and a duplicate is warned about but kept,
    /// because declaring the same run twice is a legitimate thing for a human to mean.
    /// </summary>
    public sealed partial class OrderedFileListViewModel : ViewModelBase, IFormField
    {
        private readonly IFileDialogs _dialogs;
        private readonly PickedFileKind _kind;

        [ObservableProperty]
        private OrderedFileViewModel? _selected;

        public OrderedFileListViewModel(string label, IFileDialogs dialogs, PickedFileKind kind)
        {
            Label = label ?? throw new ArgumentNullException(nameof(label));
            _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
            _kind = kind;
            Files = new ObservableCollection<OrderedFileViewModel>();
        }

        public string Label { get; }

        public ObservableCollection<OrderedFileViewModel> Files { get; }

        public int Count => Files.Count;

        public bool HasDuplicates
        {
            get
            {
                foreach (OrderedFileViewModel file in Files)
                {
                    if (file.IsDuplicate)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public string[] Paths
        {
            get
            {
                var paths = new string[Files.Count];
                for (int i = 0; i < Files.Count; i++)
                {
                    paths[i] = Files[i].Path;
                }

                return paths;
            }
        }

        /// <summary>Appends files at the end, in the order they were supplied.</summary>
        public void Append(params string[] paths)
        {
            foreach (string path in paths)
            {
                if (!string.IsNullOrWhiteSpace(path))
                {
                    Files.Add(new OrderedFileViewModel(System.IO.Path.GetFullPath(path)));
                }
            }

            Refresh();
        }

        [RelayCommand]
        private async Task AddAsync()
        {
            System.Collections.Generic.IReadOnlyList<string> picked =
                await _dialogs.PickOpenFilesAsync(Label, _kind).ConfigureAwait(true);

            if (picked.Count > 0)
            {
                Append(System.Linq.Enumerable.ToArray(picked));
            }
        }

        [RelayCommand]
        private void Remove()
        {
            if (Selected != null)
            {
                Files.Remove(Selected);
                Refresh();
            }
        }

        [RelayCommand]
        private void MoveUp() => Move(-1);

        [RelayCommand]
        private void MoveDown() => Move(1);

        private void Move(int offset)
        {
            if (Selected == null)
            {
                return;
            }

            int index = Files.IndexOf(Selected);
            int target = index + offset;
            if (index < 0 || target < 0 || target >= Files.Count)
            {
                return;
            }

            OrderedFileViewModel moved = Selected;
            Files.Move(index, target);
            Selected = moved;
            Refresh();
        }

        private void Refresh()
        {
            var seen = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < Files.Count; i++)
            {
                Files[i].Position = i + 1;
                Files[i].IsDuplicate = !seen.Add(Files[i].Path);
            }

            OnPropertyChanged(nameof(Count));
            OnPropertyChanged(nameof(HasDuplicates));
            Changed?.Invoke();
        }

        public event Action? Changed;
    }
}
