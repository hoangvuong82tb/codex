using Xunit;

// The unit suite contains SQLite-backed scheduler and retention tests that
// intentionally observe short-lived background executions. Running those
// fixtures in parallel introduces timing contention and makes the release
// gate nondeterministic; production concurrency is covered by integration
// tests instead.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
