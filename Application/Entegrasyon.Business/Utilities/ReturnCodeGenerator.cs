using System.Security.Cryptography;

namespace Entegrasyon.Business.Utilities;

public static class ReturnCodeGenerator
{
    private const string CrockfordAlphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";
    private const int PayloadLength = 13;
    private const int MaxRetries = 5;

    public static string Generate()
    {
        Span<byte> bytes = stackalloc byte[8];
        RandomNumberGenerator.Fill(bytes);

        var chars = new char[PayloadLength];
        var value = BitConverter.ToUInt64(bytes);
        for (var i = 0; i < PayloadLength; i++)
        {
            chars[i] = CrockfordAlphabet[(int)(value & 0x1F)];
            value >>= 5;
        }

        return $"R-{new string(chars)}";
    }

    public static async Task<string> GenerateUniqueAsync(Func<string, Task<bool>> existsCheck)
    {
        for (var attempt = 0; attempt < MaxRetries; attempt++)
        {
            var code = Generate();
            if (!await existsCheck(code))
                return code;
        }

        throw new InvalidOperationException(
            $"Failed to generate unique return code after {MaxRetries} attempts (collision).");
    }
}
