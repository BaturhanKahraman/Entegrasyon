using System.Text;

namespace Entegrasyon.Business.Utilities;

public static class HashingHelper
{
    public static void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
    {
        using var hmac = new System.Security.Cryptography.HMACSHA512();
        passwordSalt = hmac.Key;
        passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
    }

    public static bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt)
    {
        using var hmac = new System.Security.Cryptography.HMACSHA512(passwordSalt);
        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        for (int i = 0; i < computedHash.Length; i++)
        {
            if (computedHash[i] != passwordHash[i])
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Creates a bcrypt hash with work factor 12.</summary>
    public static string CreateBcryptHash(string password)
        => BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

    /// <summary>Verifies a password against a bcrypt hash.</summary>
    public static bool VerifyBcryptHash(string password, string hash)
        => BCrypt.Net.BCrypt.Verify(password, hash);
}
