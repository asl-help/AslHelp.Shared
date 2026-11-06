using System;
using System.Text.Json;

namespace AslHelp.Options.Profiles;

/// <summary>
///     Marks a string option as an engine version written the way the engine writes it, which is any text
///     <see cref="ProfileVersions.Validate(string?, string, string)"/> accepts.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class EngineVersionAttribute : OptionValidationAttribute
{
    /// <inheritdoc/>
    public override void Validate(object value, string origin, string path)
    {
        ArgumentNullException.ThrowIfNull(value);

        ProfileVersions.Validate(value.ToString(), origin, path);
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
        writer.WriteString("description", SR.EngineVersionDescription);
        writer.WriteEndObject();
    }
}
