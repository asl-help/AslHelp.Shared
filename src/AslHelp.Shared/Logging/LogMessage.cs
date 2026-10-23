using System;
using System.Globalization;
using System.Text;

namespace AslHelp.Logging;

/// <summary>
///     Provides the appending that every level's interpolated string handler shares.
/// </summary>
internal static class LogMessage
{
    public static StringBuilder? Started(Logger logger, LogLevel level, int literalLength, int formattedCount, out bool isEnabled)
    {
        ArgumentNullException.ThrowIfNull(logger);

        isEnabled = logger.IsEnabled(level);

        return isEnabled ? new StringBuilder(literalLength + (formattedCount * 8)) : null;
    }

    public static void Literal(StringBuilder? builder, string value)
    {
        _ = builder?.Append(value);
    }

    public static void Value<T>(StringBuilder? builder, T value)
    {
        Value(builder, value, null);
    }

    // Formats with the invariant culture, so a line reads the same on every machine, as its timestamp already does.
    public static void Value<T>(StringBuilder? builder, T value, string? format)
    {
        _ = builder?.Append(value is IFormattable formattable
            ? formattable.ToString(format, CultureInfo.InvariantCulture)
            : value?.ToString());
    }

    /// <summary>
    ///     Formats an address in hex. nint is not IFormattable on .NET Framework or netstandard2.0, so it is widened to
    ///     ulong.
    /// </summary>
    public static void Address(StringBuilder? builder, ulong value, string? format)
    {
        _ = builder?.Append(value.ToString(format, null));
    }
}
