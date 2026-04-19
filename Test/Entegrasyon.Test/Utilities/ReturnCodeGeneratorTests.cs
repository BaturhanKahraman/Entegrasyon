using Entegrasyon.Business.Utilities;

namespace Entegrasyon.UnitTest.Utilities;

public class ReturnCodeGeneratorTests
{
    [Fact]
    public void Generate_ReturnsRPrefixWith13CharPayload()
    {
        var code = ReturnCodeGenerator.Generate();

        code.Should().StartWith("R-");
        code.Length.Should().Be(15);
    }

    [Fact]
    public void Generate_UsesCrockfordAlphabet_NoForbiddenChars()
    {
        const string allowed = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

        for (var i = 0; i < 500; i++)
        {
            var code = ReturnCodeGenerator.Generate();
            var payload = code[2..];

            payload.Should().NotContainAny("I", "L", "O", "U");
            payload.All(allowed.Contains)
                   .Should().BeTrue($"payload '{payload}' contains forbidden char");
        }
    }

    [Fact]
    public async Task GenerateUniqueAsync_RetriesWhenDuplicate()
    {
        var callCount = 0;
        var returnedCodes = new List<string>();

        var code = await ReturnCodeGenerator.GenerateUniqueAsync(async generated =>
        {
            returnedCodes.Add(generated);
            callCount++;
            await Task.Yield();
            return callCount == 1;
        });

        callCount.Should().Be(2);
        returnedCodes.Should().HaveCount(2);
        code.Should().Be(returnedCodes[1]);
    }

    [Fact]
    public async Task GenerateUniqueAsync_ReturnsImmediatelyIfNoCollision()
    {
        var callCount = 0;

        var code = await ReturnCodeGenerator.GenerateUniqueAsync(_ =>
        {
            callCount++;
            return Task.FromResult(false);
        });

        callCount.Should().Be(1);
        code.Should().StartWith("R-");
    }

    [Fact]
    public async Task GenerateUniqueAsync_ThrowsAfterMaxRetries()
    {
        var act = async () => await ReturnCodeGenerator.GenerateUniqueAsync(_ => Task.FromResult(true));

        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("*collision*");
    }
}
