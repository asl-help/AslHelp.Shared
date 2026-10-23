using AslHelp.Logging;
using AslHelp.Options.Binding;
using AslHelp.Options.Profiles;
using System.Text.Json;

namespace AslHelp.Tests.Options.Profiles;

public partial class VersionedProfileCatalogueTests
{
    private sealed class FixtureCatalogue() : VersionedProfileCatalogue<FixtureProfile>("Fixture", "fixture")
    {
        public void Register(string partition, string version, int pointerSize, string document)
        {
            Add(Keyed(partition, version, pointerSize), document);
        }

        public void Register(string partition, string version, int pointerSize, FixtureProfile profile)
        {
            Add(Keyed(partition, version, pointerSize), profile);
        }

        public Result<FixtureProfile> For(
            string partition,
            string version,
            int pointerSize,
            FixtureProfile? overrides = null,
            Logger? log = null,
            string? kind = null)
        {
            if (!TryChoose(partition, version, pointerSize, out var chosen))
            {
                return NoProfile(partition, version, pointerSize);
            }

            return ResolveChosen(
                chosen,
                version,
                ProfileKey.LaneOf(pointerSize),
                $"Fixture {ProfileKey.LaneOf(pointerSize)}",
                overrides,
                log,
                kind is null ? null : $", {kind} scoped",
                kind is null ? null : profile => profile.Kind = kind);
        }

        protected override FixtureProfile Parse(string document)
        {
            return JsonSerializer.Deserialize<FixtureProfile>(document, OptionsBinder.Options)!;
        }

        protected override void Validate(FixtureProfile profile, string origin)
        {
            OptionsValidator.Require(profile, origin);
        }
    }
}
