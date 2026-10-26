using Xunit;

// Disable parallel test execution so that static storage overrides and STA WPF windows
// do not conflict between test runners.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
