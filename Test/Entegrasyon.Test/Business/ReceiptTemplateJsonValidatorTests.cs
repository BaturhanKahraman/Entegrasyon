using Entegrasyon.Business.Validation;

namespace Entegrasyon.UnitTest.Business;

public class ReceiptTemplateJsonValidatorTests
{
    [Fact]
    public void ValidateThermal_EmptyArray_IsValid()
        => ReceiptTemplateJsonValidator.ValidateThermal("[]").IsValid.Should().BeTrue();

    [Fact]
    public void ValidateThermal_SingleLogoBlock_IsValid()
    {
        var json = """[ { "id":"1", "type":"logo", "showInNormal":true, "showInGift":true, "settings":{} } ]""";
        ReceiptTemplateJsonValidator.ValidateThermal(json).IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateThermal_UnknownBlockType_IsInvalid()
    {
        var json = """[ { "id":"1", "type":"unknown_type", "showInNormal":true, "showInGift":true, "settings":{} } ]""";
        var r = ReceiptTemplateJsonValidator.ValidateThermal(json);
        r.IsValid.Should().BeFalse();
        r.Error.Should().Contain("unknown_type");
    }

    [Fact]
    public void ValidateThermal_MissingType_IsInvalid()
    {
        var json = """[ { "id":"1", "showInNormal":true, "showInGift":true, "settings":{} } ]""";
        ReceiptTemplateJsonValidator.ValidateThermal(json).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateThermal_MalformedJson_IsInvalid()
        => ReceiptTemplateJsonValidator.ValidateThermal("not json").IsValid.Should().BeFalse();

    [Fact]
    public void ValidateA4_EmptyObject_IsValid()
        => ReceiptTemplateJsonValidator.ValidateA4("{}").IsValid.Should().BeTrue();

    [Fact]
    public void ValidateA4_AllZones_IsValid()
    {
        var json = """
        { "hl": [{ "id":"a", "type":"logo", "showInNormal":true, "showInGift":true, "settings":{} }],
          "hr": [], "body": [], "fl": [], "fr": [] }
        """;
        ReceiptTemplateJsonValidator.ValidateA4(json).IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateA4_UnknownBlockInZone_IsInvalid()
    {
        var json = """{ "body": [{ "id":"1", "type":"xyz", "showInNormal":true, "showInGift":true, "settings":{} }] }""";
        ReceiptTemplateJsonValidator.ValidateA4(json).IsValid.Should().BeFalse();
    }
}
