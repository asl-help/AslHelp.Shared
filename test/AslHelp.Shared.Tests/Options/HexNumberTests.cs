using AslHelp.Options;
using NUnit.Framework;
using System.Text.Json;

namespace AslHelp.Tests.Options;

[TestFixture]
public class HexNumberTests
{
    [Test]
    public void Parse_AHexString_ReadsIt()
    {
        Assert.That(HexNumber.Parse("0x4D0"), Is.EqualTo(0x4D0));
    }

    [Test]
    public void Parse_EitherCase_ReadsTheSame()
    {
        Assert.That(HexNumber.Parse("0xab"), Is.EqualTo(HexNumber.Parse("0xAB")));
    }

    [Test]
    public void Parse_NoPrefix_RefusesWithTheCallersExpression()
    {
        string size = "58";

        var found = Assert.Throws<JsonException>(() => HexNumber.Parse(size));

        Assert.That(found!.Message, Does.Contain("size").And.Contain("0x"));
    }

    [Test]
    public void Parse_APrefixWithNoDigits_Refuses()
    {
        Assert.That(() => HexNumber.Parse("0x"), Throws.TypeOf<JsonException>());
    }

    // A value past 0x7FFFFFFF would wrap to a negative number, so it is refused with the value.
    [Test]
    public void Parse_AValuePastTheLargestInt_RefusesWithTheValue()
    {
        Assert.That(() => HexNumber.Parse("0x80000000"), Throws.TypeOf<JsonException>().With.Message.Contains("0x80000000"));
    }

    [Test]
    public void Read_ANumberPastTheLargestInt_SaysItDoesNotFit()
    {
        using var document = JsonDocument.Parse("2147483648");

        Assert.That(
            () => HexNumber.Read(document.RootElement),
            Throws.TypeOf<JsonException>().With.Message.Contains("2147483648").And.Message.Contains("32-bit"));
    }

    [Test]
    public void Parse_ADigitOutsideHex_Refuses()
    {
        Assert.That(() => HexNumber.Parse("0x5G"), Throws.TypeOf<JsonException>());
    }

    [Test]
    public void Read_ANumberOrItsHexSpelling_ReadsTheSame()
    {
        using var number = JsonDocument.Parse("32");
        using var hex = JsonDocument.Parse("\"0x20\"");

        Assert.That(HexNumber.Read(number.RootElement), Is.EqualTo(HexNumber.Read(hex.RootElement)));
    }

    [Test]
    public void Read_NeitherANumberNorAString_Refuses()
    {
        using var document = JsonDocument.Parse("true");
        var element = document.RootElement;

        var found = Assert.Throws<JsonException>(() => HexNumber.Read(element));

        Assert.That(found!.Message, Does.Contain("element"));
    }
}
