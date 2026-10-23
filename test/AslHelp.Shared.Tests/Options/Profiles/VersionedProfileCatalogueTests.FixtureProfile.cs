using AslHelp.Options;

namespace AslHelp.Tests.Options.Profiles;

public partial class VersionedProfileCatalogueTests
{
    [Options]
    private sealed class FixtureProfile
    {
        public required int? Size { get; set; }

        public string? Kind { get; set; }
    }
}
