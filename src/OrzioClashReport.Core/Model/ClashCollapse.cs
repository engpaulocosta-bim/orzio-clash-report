using System;

namespace OrzioClashReport.Core.Model
{
    /// <summary>
    /// Auditable evidence that one raw clash was collapsed into an earlier retained clash from the
    /// same clash test. Both exact source references are preserved; this record does not assert
    /// cross-run identity.
    /// </summary>
    public sealed class ClashCollapse
    {
        public string? ClashTestName { get; }
        public ClashResult RetainedClash { get; }
        public ClashResult CollapsedClash { get; }

        public ClashCollapse(string? clashTestName, ClashResult retainedClash, ClashResult collapsedClash)
        {
            ClashTestName = clashTestName;
            RetainedClash = retainedClash ?? throw new ArgumentNullException(nameof(retainedClash));
            CollapsedClash = collapsedClash ?? throw new ArgumentNullException(nameof(collapsedClash));
        }
    }
}
