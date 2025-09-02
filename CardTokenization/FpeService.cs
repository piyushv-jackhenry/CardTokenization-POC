using FPE.Net;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

internal interface IFpeService
{
    string Encrypt(string pan, string tweak);
    string Decrypt(string token, string tweak);
}

internal class FpeNetService : IFpeService
{
    private byte[] _keyBytes;
    private FF1 _ff1;

    public FpeNetService(IOptions<TokenizationOptions> options)
    {
        var keyB64 = options.Value.FpeKeyBase64
                        ?? Environment.GetEnvironmentVariable("FPE_KEY_BASE64")
                        ?? Convert.ToBase64String(Encoding.UTF8.GetBytes("FPE^KEY^BASE64"));

        if (string.IsNullOrWhiteSpace(keyB64))
            throw new ArgumentException("FPE key is not configured.");

        _keyBytes = SHA256.HashData(Encoding.UTF8.GetBytes(keyB64));

        int radix = 8;
        int maxTlen = 16;

        _ff1 = new(radix, maxTlen);
    }
    public string Encrypt(string cardNumber, string tweak)
    {
        cardNumber = new string([.. cardNumber.Where(char.IsDigit)]);
        if (cardNumber.Length < 12 || cardNumber.Length > 19) throw new ArgumentException("Card number length must be between 12 and 19 digits.", nameof(cardNumber));

        var tweakBytes = Encoding.UTF8.GetBytes(tweak);
        if (tweakBytes.Length > 16)
            tweakBytes = SHA256.HashData(tweakBytes)[..16];

        string bin = cardNumber[..6];
        string middle = cardNumber[6..^4];
        string last4 = cardNumber[^4..];

        int[] middleDigits = [.. middle.Select(c => c - '0')];
        int[] encryptedMiddleDigits = _ff1.encrypt(_keyBytes, tweakBytes, middleDigits);
        return bin + string.Concat(encryptedMiddleDigits.Select(d => d.ToString())) + last4;


        //int[] plainDigits = [.. pan.Select(c => c - '0')];
        //int[] encrypted = _ff1.encrypt(_key, _tweak, plainDigits);
        //var fpeToken = string.Concat(encrypted.Select(d => d.ToString()));
    }
    public string Decrypt(string token, string tweak)
    {
        if (!token.All(char.IsDigit) == false) throw new ArgumentException("PAN must contain only digits.", nameof(token));
        if (token.Length < 12 || token.Length > 19) throw new ArgumentException("PAN length must be between 12 and 19 digits.", nameof(token));

        var tweakBytes = Encoding.UTF8.GetBytes(tweak);
        if (tweakBytes.Length > 16)
            tweakBytes = SHA256.HashData(tweakBytes)[..16];

        string bin = token[..6];
        string middle = token[6..^4];
        string last4 = token[(token.Length - 4)..4];

        int[] middleDigits = [.. middle.Select(c => c - '0')];
        int[] decryptedMiddleDigits = _ff1.decrypt(_keyBytes, tweakBytes, middleDigits);

        return bin + string.Concat(decryptedMiddleDigits.Select(d => d.ToString())) + last4;
    }
}