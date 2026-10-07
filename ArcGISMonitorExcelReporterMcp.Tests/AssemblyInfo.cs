using Xunit;

// ConfigurationLoaderHttpModeTests toggles the static ConfigurationLoader.AllowConfigPath flag,
// which every other test class implicitly relies on staying true. Disable cross-class
// parallelization so that toggle can't race with configPath-based tests in other classes.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
