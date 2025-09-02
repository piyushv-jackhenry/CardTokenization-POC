
using System.Security.Cryptography;
using System.Text;

public static class Extensions
{

    public static string ComputeHash(this string input, string algorithm = "SHA256")
    {
        using HashAlgorithm hashAlg = algorithm.ToUpper() switch
        {
            "SHA1" => SHA1.Create(),
            "MD5" => MD5.Create(),
            _ => SHA256.Create() // Default to SHA256
        };

        byte[] inputBytes = Encoding.UTF8.GetBytes(input);
        byte[] hashBytes = hashAlg.ComputeHash(inputBytes);

        return Convert.ToHexString(hashBytes);
    }
}

