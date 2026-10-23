using System;

namespace AslHelp.Options;

/// <summary>
///     Marks an options type: hand-written source declaring which options exist and what they default to.
/// </summary>
/// <remarks>
///     Membership follows the serializer's own rules: public instance properties, fields opted in with
///     <c>[JsonInclude]</c>, <c>[JsonIgnore]</c> excluding and <c>[JsonPropertyName]</c> renaming. A member whose type
///     is itself marked nests as a section. Any other member is a leaf, whose type must be serializable or have a
///     converter. The generator emits the type's <c>Shape</c>, which reports an unknown option together with the
///     options that exist.
/// </remarks>
[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Struct,
    AllowMultiple = false,
    Inherited = false)]
public sealed class OptionsAttribute : Attribute;
