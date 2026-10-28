using AslHelp.Options.Binding;
using System;
using System.Text.Json;

namespace AslHelp.Options;

/// <summary>
///     Marks an option whose value has a shape the binder cannot check. The mark provides the check of that shape and
///     the schema that describes it.
/// </summary>
/// <remarks>
///     <see cref="OptionsValidator.ValidateValues(object, string)"/> calls <see cref="Validate(object, string, string)"/> on
///     every marked option that has a value, however the options were set. The generated schema writes the option
///     through <see cref="WriteSchema(Utf8JsonWriter)"/>, so a marked option is checked and described in 1 place.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public abstract class OptionValidationAttribute : Attribute
{
    /// <summary>
    ///     Checks a value an options source set.
    /// </summary>
    /// <param name="value">The value, never <see langword="null"/>.</param>
    /// <param name="origin">Where the value came from, written into the error.</param>
    /// <param name="path">The option's full path, written into the error.</param>
    /// <exception cref="JsonException">
    ///     <paramref name="value"/> does not have the shape the option takes.
    /// </exception>
    public abstract void Validate(object value, string origin, string path);

    /// <summary>
    ///     Writes the schema of a value the option takes.
    /// </summary>
    /// <param name="writer">The writer to write the schema object into.</param>
    public abstract void WriteSchema(Utf8JsonWriter writer);
}
