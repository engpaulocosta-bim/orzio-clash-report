using Xunit;

// The interface language is one process-wide setting, and these tests are written against Portuguese
// unless a test says otherwise. Running test classes one after another keeps a test that switches to
// English from changing the text another test is asserting on.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
