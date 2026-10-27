using System;

namespace AslHelp.Options;

/// <summary>
///     Registers another spelling <see cref="Choice.Parse{TEnum}(string, string, string)"/> accepts for the member it
///     marks, alongside its own kebab case name.
/// </summary>
/// <param name="alias">The other spelling.</param>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class ChoiceAliasAttribute(
    string alias) : Attribute
{
    /// <summary>
    ///     Gets the other spelling.
    /// </summary>
    public string Alias { get; } = alias;
}
