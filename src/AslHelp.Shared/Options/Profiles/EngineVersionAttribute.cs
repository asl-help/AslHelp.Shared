using System;
using System.Text.Json;

namespace AslHelp.Options.Profiles;

/// <summary>
///     Marks a string option as an engine version written the way the engine writes it, which is any text
///     <see cref="ProfileVersions.Check(string?, string, string)"/> accepts.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class EngineVersionAttribute : OptionCheckAttribute
{
    private const string Description = "An engine version the way the engine spells it, such as \"2022.3.5f1\", \"4.24\" or \"3.8225\".";

    /// <inheritdoc/>
    public override void Check(object value, string origin, string path)
    {
        ArgumentNullException.ThrowIfNull(value);

        ProfileVersions.Check(value.ToString(), origin, path);
    }

    /// <inheritdoc/>
    /// <remarks>
    ///     The schema accepts the version text or <c>null</c> for a version read from the target at run time.
    /// </remarks>
    public override void WriteSchema(Utf8JsonWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        writer.WriteStartObject();
        writer.WriteStartArray("type");
        writer.WriteStringValue("string");
        writer.WriteStringValue("null");
        writer.WriteEndArray();
        writer.WriteString("pattern", ProfileVersions.Pattern);
        writer.WriteString("description", Description);
        writer.WriteEndObject();
    }
}
