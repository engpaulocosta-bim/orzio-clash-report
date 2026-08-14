namespace OrzioClashReport.Launcher.Contracts.Operations
{
    /// <summary>
    /// What a human decided about a destination that already exists. There is no default that
    /// replaces a file: <see cref="None"/> means no decision has been taken, and the operation stops.
    /// </summary>
    public enum OutputCollisionDecision
    {
        /// <summary>No human decision yet. The launcher refuses to run.</summary>
        None = 0,

        /// <summary>The user will pick a different destination.</summary>
        ChooseAnotherName = 1,

        /// <summary>
        /// The user explicitly authorised replacing an existing report. This is only ever offered for
        /// derived, regenerable HTML; snapshots, run indexes, project catalogs and governance documents
        /// are created with create-new semantics inside the engine and are never replaced from here.
        /// </summary>
        ReplaceExisting = 2,
    }
}
