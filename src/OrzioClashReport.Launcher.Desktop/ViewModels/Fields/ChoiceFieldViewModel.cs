using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using OrzioClashReport.Launcher.Application.Presentation;
using OrzioClashReport.Launcher.Desktop.Localization;

namespace OrzioClashReport.Launcher.Desktop.ViewModels.Fields
{
    /// <summary>
    /// A choice between a small set of options, each carrying its own glyph and label so the selected
    /// option is never identified by colour alone.
    /// </summary>
    public sealed partial class ChoiceFieldViewModel : OperationFieldViewModel
    {
        private readonly Action? _onChanged;

        private bool _isUpdating;

        [ObservableProperty]
        private ChoiceOptionViewModel? _selected;

        public ChoiceFieldViewModel(
            string labelKey,
            string descriptionKey,
            IReadOnlyList<ChoiceOptionViewModel> options,
            Action? onChanged = null)
            : base(labelKey, descriptionKey)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (options.Count == 0)
            {
                throw new ArgumentException("A choice needs at least one option.", nameof(options));
            }

            foreach (ChoiceOptionViewModel option in options)
            {
                option.PropertyChanged += OnOptionChanged;
                Options.Add(option);
            }

            _onChanged = onChanged;

            // Deliberately unselected: a decision this consequential is never pre-made for the user.
            Selected = null;
        }

        public ObservableCollection<ChoiceOptionViewModel> Options { get; } =
            new ObservableCollection<ChoiceOptionViewModel>();

        public override bool IsComplete => Selected != null;

        public void Select(ChoiceOptionViewModel option) => option.IsSelected = true;

        private void OnOptionChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (_isUpdating
                || e.PropertyName != nameof(ChoiceOptionViewModel.IsSelected)
                || sender is not ChoiceOptionViewModel changed
                || !changed.IsSelected)
            {
                return;
            }

            _isUpdating = true;
            try
            {
                foreach (ChoiceOptionViewModel option in Options)
                {
                    if (!ReferenceEquals(option, changed))
                    {
                        option.IsSelected = false;
                    }
                }

                Selected = changed;
            }
            finally
            {
                _isUpdating = false;
            }
        }

        partial void OnSelectedChanged(ChoiceOptionViewModel? value)
        {
            OnPropertyChanged(nameof(IsComplete));
            _onChanged?.Invoke();
        }
    }

    /// <summary>One option of a <see cref="ChoiceFieldViewModel"/>.</summary>
    public sealed partial class ChoiceOptionViewModel : LocalizedObservableObject
    {
        private readonly Func<string> _label;
        private readonly Func<string> _description;

        [ObservableProperty]
        private bool _isSelected;

        /// <param name="value">The canonical value sent to the engine. Never translated.</param>
        /// <param name="label">Reads the option's label in the active language, each time it is needed.</param>
        /// <param name="description">Reads the option's description in the active language.</param>
        public ChoiceOptionViewModel(
            string value, Func<string> label, string glyph, Func<string> description, LauncherSeverity severity)
        {
            Value = value;
            _label = label ?? throw new ArgumentNullException(nameof(label));
            Glyph = glyph;
            _description = description ?? throw new ArgumentNullException(nameof(description));

            IsPositive = severity == LauncherSeverity.Positive;
            IsCaution = severity == LauncherSeverity.Caution;
            IsCritical = severity == LauncherSeverity.Critical;
            IsNeutral = severity == LauncherSeverity.Neutral;
        }

        /// <summary>The canonical value sent to the engine. Never localised.</summary>
        public string Value { get; }

        public string Label => _label();

        public string Glyph { get; }

        public string Description => _description();

        public bool IsPositive { get; }

        public bool IsCaution { get; }

        public bool IsCritical { get; }

        public bool IsNeutral { get; }
    }
}
