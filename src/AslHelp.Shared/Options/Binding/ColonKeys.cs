using AslHelp.Options;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AslHelp.Options.Binding;

/// <summary>
///     Expands the colon grammar: <c>"a:b": v</c> is short for <c>"a": { "b": v }</c>, so a document sets 1 nested
///     option without opening the objects above it.
/// </summary>
/// <remarks>
///     The separator is the one Microsoft.Extensions.Configuration uses for configuration paths, so the form matches
///     the .NET configuration convention. The expansion runs once over a whole document before anything binds, so every
///     section gets the grammar and no binder ever sees a colon. An expanded key merges into a sibling that spells the
///     objects out. Reaching the same leaf more than once throws because a value set in 2 places is a mistake.
/// </remarks>
public static class ColonKeys
{
    /// <summary>
    ///     Expands every colon key in <paramref name="node"/>, recursively.
    /// </summary>
    /// <param name="node">The node to expand in place.</param>
    /// <exception cref="JsonException">
    ///     A colon key reaches through a value that is not an object or reaches a leaf another key already set.
    /// </exception>
    public static void Expand(JsonNode? node)
    {
        if (node is not JsonObject parent)
        {
            return;
        }

        // Expanding mutates `parent`, so the loop iterates over a copy.
        var properties = new List<KeyValuePair<string, JsonNode?>>(parent);

        foreach (var property in properties)
        {
            Expand(property.Value);

            int colon = property.Key.IndexOf(':');
            if (colon < 0)
            {
                continue;
            }

            _ = parent.Remove(property.Key);
            Insert(parent, property.Key, property.Key[..colon], property.Key[(colon + 1)..], property.Value);
        }
    }

    private static void Insert(JsonObject parent, string key, string name, string remainder, JsonNode? value)
    {
        if (name.Length == 0 || remainder.Length == 0)
        {
            throw new JsonException($"The key '{key}' is missing a name around a ':'.");
        }

        if (!parent.TryGetPropertyValue(name, out var below))
        {
            below = new JsonObject();
            parent[name] = below;
        }

        if (below is not JsonObject inner)
        {
            throw new JsonException($"The key '{key}' reaches into '{name}', whose value is not an object.");
        }

        int colon = remainder.IndexOf(':');

        if (colon >= 0)
        {
            Insert(inner, key, remainder[..colon], remainder[(colon + 1)..], value);
            return;
        }

        if (inner.ContainsKey(remainder))
        {
            throw new JsonException($"The key '{key}' sets '{remainder}', which '{name}' already sets.");
        }

        inner[remainder] = value?.DeepClone();
    }
}
