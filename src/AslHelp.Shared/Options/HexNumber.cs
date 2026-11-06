using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace AslHelp.Options;

/// <summary>
///     Reads a whole number written as a json number or as a <c>"0x"</c> hex string, which is how an offset reads most
///     naturally in a document json has no hex literal for.
/// </summary>
public static class HexNumber
{
    /// <summary>
    ///     Reads a <c>"0x"</c> hex string.
    /// </summary>
    /// <param name="text">The text to read.</param>
    /// <param name="paramName">What to call the value in a failure, defaulting to the caller's own expression.</param>
    /// <returns>
    ///     The number the text spells.
    /// </returns>
    /// <exception cref="JsonException">
    ///     <paramref name="text"/> is not a <c>"0x"</c> hex string.
    /// </exception>
    public static int Parse(string text, [CallerArgumentExpression(nameof(text))] string? paramName = null)
    {
        // Accepts exactly what the schema allows, which is `^0x[a-fA-F0-9]+$`.
        if (text?.StartsWith("0x", StringComparison.Ordinal) != true)
        {
            throw new JsonException(SR.FormatHexPrefixMissing(paramName, text));
        }

        if (!long.TryParse(text[2..], NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out long value))
        {
            throw new JsonException(SR.FormatNotAHexNumber(paramName, text));
        }

        // A 32-bit hex value past 0x7FFFFFFF would read as a negative offset, so it is refused rather than wrapped.
        if (value is < 0 or > int.MaxValue)
        {
            throw new JsonException(SR.FormatHexNumberTooLarge(paramName, text));
        }

        return (int)value;
    }

    /// <summary>
    ///     Reads a member written either way.
    /// </summary>
    /// <param name="element">The member to read.</param>
    /// <param name="paramName">What to call the value in a failure, defaulting to the caller's own expression.</param>
    /// <returns>
    ///     The number in the member.
    /// </returns>
    /// <exception cref="JsonException">
    ///     <paramref name="element"/> is neither a number nor a <c>"0x"</c> hex string.
    /// </exception>
    public static int Read(JsonElement element, [CallerArgumentExpression(nameof(element))] string? paramName = null)
    {
        if (element.ValueKind == JsonValueKind.Number)
        {
            return element.TryGetInt32(out int number)
                ? number
                : throw new JsonException(SR.FormatNumberOutOfRange(paramName, element.GetRawText()));
        }

        if (element.ValueKind != JsonValueKind.String)
        {
            throw new JsonException(SR.FormatNotANumberOrHexString(paramName));
        }

        return Parse(element.GetString()!, paramName);
    }
}
