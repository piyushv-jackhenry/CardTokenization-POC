using System.Security.Cryptography;
using System.Text;

internal interface ITokenizationService
{
    TokenEntity TokenizeAsync(string cardNumber, bool createIfNotFound);
    TokenEntity DetokenizeAsync(string token);
}

internal class TokenizationService : ITokenizationService
{
    private readonly IFpeService _fpe;
    public TokenizationService(IFpeService fpe)
    {
        _fpe = fpe;
    }

    public TokenEntity TokenizeAsync(string cardNumber, bool createIfNotFound)
    {
        var digits = new string([.. cardNumber.Where(char.IsDigit)]);
        if (digits.Length < 12 || digits.Length > 19) throw new ArgumentException("Card number length must be between 12 and 19 digits.", nameof(cardNumber));

        var opaqueToken = CreateToken();
        var fpeToken = _fpe.Encrypt(digits, opaqueToken);

        return new TokenEntity
        {
            CardNumber = fpeToken,
            Token = opaqueToken,
            CreatedUtc = DateTime.UtcNow
        };
    }

    public TokenEntity DetokenizeAsync(string token)
    {
        TokenEntity entity = null;
        if (token.All(char.IsDigit))
        {
            entity = null; // lookup by FPE token
        }
        else
        {
            entity = null; // lookup by opaque token
        }

        if (entity == null)
            return null;

        var pan = _fpe.Decrypt(entity.CardNumber, "");

        return new TokenEntity
        {
            CardNumber = pan,
            Token = entity.Token,
            CreatedUtc = entity.CreatedUtc
        };
    }

    private string CreateToken()
    {
        byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()));
        string hashHex = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        return  "T" + hashHex[..18];
    }

    public static bool IsValidLuhn(string number)
    {
        int sum = 0;
        bool alternate = false;

        for (int i = number.Length - 1; i >= 0; i--)
        {
            int n = int.Parse(number[i].ToString());
            if (alternate)
            {
                n *= 2;
                if (n > 9) n -= 9;
            }
            sum += n;
            alternate = !alternate;
        }

        return (sum % 10 == 0);
    }
}