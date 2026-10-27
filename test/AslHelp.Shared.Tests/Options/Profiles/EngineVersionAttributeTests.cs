using AslHelp.Options;
using AslHelp.Options.Binding;
using AslHelp.Options.Profiles;
using NUnit.Framework;
using System.IO;
using System.Text;
using System.Text.Json;

namespace AslHelp.Tests.Options.Profiles;

[TestFixture]
public class EngineVersionAttributeTests
{
    [Test]
    public void ValidateValues_AnEngineVersionOrNone_PassesIt()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(() => OptionsValidator.ValidateValues(new FixtureVersioned { Version = "2022.3.5f1" }, "Initialize"), Throws.Nothing);
            Assert.That(() => OptionsValidator.ValidateValues(new FixtureVersioned(), "Initialize"), Throws.Nothing);
        }
    }

    [Test]
    public void ValidateValues_AnEngineVersionThatIsNoVersion_ThrowsWithTheOptionAndTheValue()
    {
        Assert.That(
            () => OptionsValidator.ValidateValues(new FixtureVersioned { Version = "latest" }, "Initialize"),
            Throws.TypeOf<JsonException>().With.Message.Contains("'version'").And.Message.Contains("'latest'"));
    }

    // The schema accepts null as well because a version left unset is read from the target at run time.
    [Test]
    public void WriteSchema_TheMark_WritesANullableStringWithThePattern()
    {
        using MemoryStream stream = new();

        using (Utf8JsonWriter writer = new(stream))
        {
            new EngineVersionAttribute().WriteSchema(writer);
        }

        using var schema = JsonDocument.Parse(Encoding.UTF8.GetString(stream.ToArray()));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(schema.RootElement.GetProperty("type").ToString(), Is.EqualTo("[\"string\",\"null\"]"));
            Assert.That(schema.RootElement.GetProperty("pattern").GetString(), Is.EqualTo(ProfileVersions.Pattern));
        }
    }

    [Options]
    private sealed class FixtureVersioned
    {
        [EngineVersion]
        public string? Version { get; set; }
    }
}
