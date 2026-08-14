using CommunityToolkit.Mvvm.ComponentModel;

namespace OrzioClashReport.Launcher.Desktop.ViewModels
{
    /// <summary>
    /// Enforces one active job per window. Two engine runs writing at once could collide on the same
    /// destination or on the same project tree, and neither the user nor the log would be able to say
    /// which one did what.
    /// </summary>
    public sealed partial class ActiveJobTracker : ObservableObject
    {
        [ObservableProperty]
        private bool _isBusy;

        /// <summary>Returns false when another job already holds the window.</summary>
        public bool TryAcquire()
        {
            if (IsBusy)
            {
                return false;
            }

            IsBusy = true;
            return true;
        }

        public void Release() => IsBusy = false;
    }
}
