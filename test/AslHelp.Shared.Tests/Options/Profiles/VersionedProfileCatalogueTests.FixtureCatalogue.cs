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
            Add(CreateKey(partition, version, pointerSize), document);
        }

        public void Register(string partition, string version, int pointerSize, FixtureProfile profile)
        {
            Add(CreateKey(partition, version, pointerSize), profile);
        }

        public void RegisterFallbacks(string document)
        {
            AddFallbacks(document);
        }

        public Result<FixtureProfile> Resolve(
            string partition,
            string version,
            int pointerSize,
            FixtureProfile? overrides = null,
            Logger? log = null,
            string? kind = null)
        {
            if (!TryFindKey(partition, version, pointerSize, out var key))
            {
                return CreateNoProfileError(partition, version, pointerSize);
            }

            return Resolve(
                key,
                version,
                ProfileKey.GetArchitectureName(pointerSize),
                $"Fixture {ProfileKey.GetArchitectureName(pointerSize)}",
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
            OptionsValidator.ThrowIfMissingRequired(profile, origin);
        }
    }
}
